using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Records;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Services.BlobStorageService
{
    // Options used for both Azure Blob Storage and Azurite emulator.
    public sealed class BlobStorageOptions
    {
        // Full connection string. For Azurite you can use:
        // "UseDevelopmentStorage=true"
        // or explicit:
        // "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vd...==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
        public string ConnectionString { get; init; } = string.Empty;
        public string ContainerName { get; init; } = "photos";
        public string? PublicBaseUrl { get; init; }
        public bool AutoCreateContainer { get; init; } = true;
        public bool PublicAccess { get; init; } = true;
        public string? BlobPrefix { get; init; }
        public string UsageMetadataKey { get; init; } = "inUse";
    }

    public interface IPhotoReferenceProvider
    {
        Task<IReadOnlyCollection<string>> GetAllReferencedBlobNamesAsync(CancellationToken cancellationToken);
    }

    public class AzureBlobPhotoService : IPhotoService
    {
        private readonly BlobContainerClient _container;
        private readonly BlobStorageOptions _options;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
        private volatile bool _containerInitialized;
        private readonly SemaphoreSlim _containerInitLock = new(1, 1);
        private readonly IPhotoReferenceProvider? _referenceProvider;
        private readonly ILogger<AzureBlobPhotoService>? _logger;

        private readonly ConcurrentDictionary<string, bool> _usageCache = new();

        public AzureBlobPhotoService(BlobStorageOptions options, IPhotoReferenceProvider? referenceProvider, ILogger<AzureBlobPhotoService>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new ArgumentException("Blob storage connection string is not configured.");
            }
            var blobServiceClient = new BlobServiceClient(_options.ConnectionString);
            _container = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            _referenceProvider = referenceProvider;
            _logger = logger;
        }

        public async Task<PhotoUploadResult> UploadPhotoAsync(byte[] photoBytes, string fileName, CancellationToken cancellationToken)
        {
            if (photoBytes is null || photoBytes.Length == 0)
                throw new ArgumentException("Порожній вміст файлу.", nameof(photoBytes));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Порожня назва файлу.", nameof(fileName));

            await EnsureContainerAsync(cancellationToken).ConfigureAwait(false);

            var (processedBytes, finalExtension) = await CompressImageAsync(photoBytes, fileName, cancellationToken).ConfigureAwait(false);

            var blobName = BuildBlobName(finalExtension);
            var blobClient = _container.GetBlobClient(blobName);

            var headers = new BlobHttpHeaders
            {
                ContentType = ResolveContentType(fileName, finalExtension) ?? "image/jpeg",
                CacheControl = "public, max-age=31536000"
            };

            await using var ms = new MemoryStream(processedBytes);
            await blobClient.UploadAsync(ms, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken)
                .ConfigureAwait(false);

            var url = BuildPublicUrl(blobClient);
            return new PhotoUploadResult(blobName, url);
        }

        internal async Task<(byte[] ProcessedBytes, string FinalExtension)> CompressImageAsync(byte[] photoBytes, string fileName, CancellationToken cancellationToken)
        {
            var originalSize = photoBytes.Length;
            string finalExtension = Path.GetExtension(fileName).ToLowerInvariant();

            try
            {
                using var image = Image.Load(photoBytes);
                
                // Resize if too large (max 1920x1920)
                const int MaxDimension = 1920;
                if (image.Width > MaxDimension || image.Height > MaxDimension)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(MaxDimension, MaxDimension)
                    }));
                }

                // Compress and convert to JPEG to ensure small size (Quality 75 is a good balance)
                using var msCompressed = new MemoryStream();
                var encoder = new JpegEncoder { Quality = 75 };
                await image.SaveAsync(msCompressed, encoder, cancellationToken).ConfigureAwait(false);
                
                byte[] processedBytes = msCompressed.ToArray();
                
                _logger?.LogInformation("Image {FileName} compressed: {OriginalSize} bytes -> {CompressedSize} bytes (Saved {SavedBytes} bytes)", 
                    fileName, originalSize, processedBytes.Length, originalSize - processedBytes.Length);

                return (processedBytes, ".jpg"); // Force extension to jpg since we encoded as jpeg
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to compress image {FileName}. Proceeding with original bytes.", fileName);
                // If it's not a valid image (e.g. corrupted), we fallback to original bytes
                // The API shouldn't accept non-images, but this is a fallback.
                finalExtension = string.IsNullOrWhiteSpace(finalExtension) ? ".bin" : finalExtension;
                return (photoBytes, finalExtension);
            }
        }

        public async Task<bool> DeletePhotoAsync(string photoUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(photoUrl))
                return false;

            string blobName;
            if (photoUrl.Contains("://"))
            {
                if (!TryExtractBlobName(photoUrl, out blobName))
                    return false;
            }
            else
            {
                blobName = photoUrl;
            }

            await EnsureContainerAsync(cancellationToken).ConfigureAwait(false);
            var blobClient = _container.GetBlobClient(blobName);
            try
            {
                var resp = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetOrphanFilesAsync(CancellationToken cancellationToken)
        {
            if (_referenceProvider is null)
                return Array.Empty<string>();

            await EnsureContainerAsync(cancellationToken).ConfigureAwait(false);

            var referenced = await _referenceProvider.GetAllReferencedBlobNamesAsync(cancellationToken).ConfigureAwait(false);
            var referencedSet = new HashSet<string>(referenced, StringComparer.Ordinal);

            var allBlobs = new List<BlobItem>();
            await foreach (var item in _container.GetBlobsAsync(
                               traits: BlobTraits.None,
                               states: BlobStates.None,
                               prefix: _options.BlobPrefix,
                               cancellationToken))
            {
                allBlobs.Add(item);
            }

            var orphans = allBlobs
                .Where(item => !referencedSet.Contains(item.Name))
                .Select(item => BuildPublicUrl(_container.GetBlobClient(item.Name)))
                .ToList();

            return orphans;
        }

        // Helper that could be invoked externally (not part of interface) to mark a blob as in-use again.
        public async Task MarkInUseAsync(string photoUrl, bool inUse, CancellationToken cancellationToken)
        {
            if (!TryExtractBlobName(photoUrl, out var blobName))
                return;

            await EnsureContainerAsync(cancellationToken).ConfigureAwait(false);

            var blobClient = _container.GetBlobClient(blobName);
            try
            {
                var props = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                var metadata = props.Value.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                metadata[_options.UsageMetadataKey] = inUse ? "true" : "false";
                await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
                _usageCache[blobName] = inUse;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Ignore missing blob.
            }
        }

        private async Task EnsureContainerAsync(CancellationToken cancellationToken)
        {
            if (_containerInitialized)
                return;

            await _containerInitLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_containerInitialized)
                    return;

                if (_options.AutoCreateContainer)
                {
                    await _container.CreateIfNotExistsAsync(
                        _options.PublicAccess
                            ? PublicAccessType.Blob
                            : PublicAccessType.None,
                        cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
                }

                _containerInitialized = true;
            }
            finally
            {
                _containerInitLock.Release();
            }
        }

        private string BuildBlobName(string extension)
        {
            // Photos/<prefix>/yyyy/MM/dd/<guid>.ext
            var now = DateTime.UtcNow;
            var datePath = $"{now:yyyy/MM/dd}";
            var prefix = string.IsNullOrWhiteSpace(_options.BlobPrefix)
                ? $"photos/{datePath}"
                : $"{_options.BlobPrefix.TrimEnd('/')}/{datePath}";
            return $"{prefix}/{Guid.NewGuid():N}{extension}";
        }

        // Select blobName from full URL (container/name...). Azure/Azurite
        private static bool TryExtractBlobName(string photoUrl, out string blobName)
        {
            blobName = string.Empty;
            if (!Uri.TryCreate(photoUrl, UriKind.Absolute, out var uri))
                return false;

            // /<container>/<blobName>
            var parts = uri.AbsolutePath.Trim('/').Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            blobName = parts[1];
            return true;
        }

        private string BuildPublicUrl(BlobClient client)
        {
            if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
                return $"{_options.PublicBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(client.Name)}";
            return client.Uri.ToString();
        }

        private string? ResolveContentType(string fileName, string extension)
        {
            if (_contentTypeProvider.TryGetContentType(fileName, out var ct))
                return ct;
            if (_contentTypeProvider.TryGetContentType("file" + extension, out var extCt))
                return extCt;
            return null;
        }
    }
}
