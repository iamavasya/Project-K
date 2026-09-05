using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

/// <summary>One stored image, as the cleanup report sees it.</summary>
public sealed record StoredAnnouncementImage(string Key, DateTimeOffset? LastModified);

public interface IPublicAnnouncementImageStore
{
    Task<PublicAnnouncementImageUploadResult> SaveAsync(
        byte[] imageBytes,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken);

    Task<PublicAnnouncementImageFile?> OpenAsync(string imageKey, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string imageKey, CancellationToken cancellationToken);

    /// <summary>
    /// Every image the store holds, for the orphan report. <c>LastModified</c> is what the grace
    /// period is measured against and may be absent when the backing store does not record it.
    /// </summary>
    Task<IReadOnlyList<StoredAnnouncementImage>> ListAsync(CancellationToken cancellationToken = default);
}
