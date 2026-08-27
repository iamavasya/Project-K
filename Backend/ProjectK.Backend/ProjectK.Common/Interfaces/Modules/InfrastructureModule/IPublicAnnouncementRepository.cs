using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

public interface IPublicAnnouncementRepository : IBaseEntityRepository<PublicAnnouncementDraft>
{
    Task<IReadOnlyCollection<PublicAnnouncementDraft>> GetByStatusAsync(
        PublicAnnouncementStatus? status,
        CancellationToken cancellationToken = default);

    /// <summary>Image keys still referenced by a draft, so the rest can be reported as orphans.</summary>
    Task<IReadOnlyCollection<string>> GetReferencedImageKeysAsync(CancellationToken cancellationToken = default);

    Task<PublicAnnouncementDraft?> GetActiveBySourceAsync(
        PublicAnnouncementSourceType sourceType,
        string sourceId,
        Guid? exceptDraftKey = null,
        CancellationToken cancellationToken = default);
}
