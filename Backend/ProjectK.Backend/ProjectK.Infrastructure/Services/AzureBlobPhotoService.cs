using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Services
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
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _container;
        private readonly BlobStorageOptions _options;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
        private volatile bool _containerInitialized;
        private readonly SemaphoreSlim _containerInitLock = new(1, 1);
        private readonly IPhotoReferenceProvider? _referenceProvider;

        private readonly ConcurrentDictionary<string, bool> _usageCache = new();

        public AzureBlobPhotoService(BlobStorageOptions options, IPhotoReferenceProvider? referenceProvider)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new ArgumentException("Blob storage connection string is not configured.");
            }
            _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
            _container = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
            _referenceProvider = referenceProvider;
        }
        public async Task<PhotoUploadResult> UploadPhotoAsync(byte[] photoBytes, string fileName, CancellationToken cancellationToken)
        {
            if (photoBytes is null || photoBytes.Length == 0)
                throw new ArgumentException("Порожній вміст файлу.", nameof(photoBytes));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Порожня назва файлу.", nameof(fileName));

            await EnsureContainerAsync(cancellationToken).ConfigureAwait(false);

            var ext = Path.GetExtension(fileName);
            var safeExt = string.IsNullOrWhiteSpace(ext) ? ".bin" : ext.ToLowerInvariant();
            var blobName = BuildBlobName(safeExt);
            var blobClient = _container.GetBlobClient(blobName);

            var headers = new BlobHttpHeaders
            {
                ContentType = ResolveContentType(fileName, safeExt) ?? "application/octet-stream",
                CacheControl = "public, max-age=31536000"
            };

            await using var ms = new MemoryStream(photoBytes);
            await blobClient.UploadAsync(ms, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken)
                .ConfigureAwait(false);

            var url = BuildPublicUrl(blobClient);
            return new PhotoUploadResult(blobName, url);
        }

        public async Task<bool> DeletePhotoAsync(string photoUrlOrBlobRef, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(photoUrlOrBlobRef))
                return false;

            string blobName;
            if (photoUrlOrBlobRef.Contains("://"))
            {
                if (!TryExtractBlobName(photoUrlOrBlobRef, out blobName))
                    return false;
            }
            else
            {
                blobName = photoUrlOrBlobRef;
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

            var orphans = new List<string>();

            await foreach (var item in _container.GetBlobsAsync(
                               traits: BlobTraits.None,
                               states: BlobStates.None,
                               prefix: _options.BlobPrefix,
                               cancellationToken))
            {
                // item.Name – blobName (relative path in container)
                if (!referencedSet.Contains(item.Name))
                {
                    var client = _container.GetBlobClient(item.Name);
                    orphans.Add(BuildPublicUrl(client));
                }
            }

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
