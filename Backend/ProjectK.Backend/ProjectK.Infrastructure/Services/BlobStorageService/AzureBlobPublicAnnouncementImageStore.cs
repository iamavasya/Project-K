using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Records;
using ProjectK.Infrastructure.Services.PublicAnnouncements;

namespace ProjectK.Infrastructure.Services.BlobStorageService
{
    public sealed class AzureBlobPublicAnnouncementImageStore : IPublicAnnouncementImageStore
    {
        private const string ContentType = "image/jpeg";

        private readonly BlobContainerClient _container;
        private readonly IPhotoService _photoService;
        private readonly LocalPublicAnnouncementImageStore? _legacyStore;

        public AzureBlobPublicAnnouncementImageStore(
            BlobStorageOptions options,
            IPhotoService photoService,
            LocalPublicAnnouncementImageStore? legacyStore = null)
        {
            var blobServiceClient = new BlobServiceClient(options.ConnectionString);
            _container = blobServiceClient.GetBlobContainerClient(options.ContainerName);
            _photoService = photoService;
            _legacyStore = legacyStore;
        }

        public async Task<PublicAnnouncementImageUploadResult> SaveAsync(
            byte[] imageBytes,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken)
        {
            var upload = await _photoService.UploadPhotoAsync(
                    imageBytes,
                    fileName,
                    BlobUploadContext.PublicAnnouncement,
                    cancellationToken)
                .ConfigureAwait(false);

            return new PublicAnnouncementImageUploadResult(
                upload.BlobName,
                Path.GetFileName(upload.BlobName),
                ContentType);
        }

        public async Task<PublicAnnouncementImageFile?> OpenAsync(string imageKey, CancellationToken cancellationToken)
        {
            if (!TryNormalizeKey(imageKey, out var normalizedKey))
                return _legacyStore is null
                    ? null
                    : await _legacyStore.OpenAsync(imageKey, cancellationToken).ConfigureAwait(false);

            var blobClient = _container.GetBlobClient(normalizedKey);

            try
            {
                var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return new PublicAnnouncementImageFile(
                    download.Value.Content,
                    Path.GetFileName(normalizedKey),
                    download.Value.Details.ContentType ?? ContentType);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string imageKey, CancellationToken cancellationToken)
        {
            if (!TryNormalizeKey(imageKey, out var normalizedKey))
                return _legacyStore is not null
                    && await _legacyStore.DeleteAsync(imageKey, cancellationToken).ConfigureAwait(false);

            var blobClient = _container.GetBlobClient(normalizedKey);
            var response = await blobClient.DeleteIfExistsAsync(
                    DeleteSnapshotsOption.IncludeSnapshots,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Value;
        }

        private static bool TryNormalizeKey(string imageKey, out string normalizedKey)
        {
            normalizedKey = imageKey.Trim().TrimStart('/');
            return normalizedKey.StartsWith($"{BlobUploadFolders.PublicAnnouncements}/", StringComparison.Ordinal)
                && !normalizedKey.Contains('\\')
                && !normalizedKey.Contains("..", StringComparison.Ordinal)
                && normalizedKey.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase);
        }
    }
}
