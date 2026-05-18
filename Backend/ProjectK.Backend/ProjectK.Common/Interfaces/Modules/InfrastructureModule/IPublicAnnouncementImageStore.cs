using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

public interface IPublicAnnouncementImageStore
{
    Task<PublicAnnouncementImageUploadResult> SaveAsync(
        byte[] imageBytes,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken);

    Task<PublicAnnouncementImageFile?> OpenAsync(string imageKey, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string imageKey, CancellationToken cancellationToken);
}
