using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories;

public class PublicAnnouncementRepository : BaseEntityRepository<PublicAnnouncementDraft>, IPublicAnnouncementRepository
{

    public PublicAnnouncementRepository(AppDbContext context) : base(context)
        {
        }

    public override async Task<PublicAnnouncementDraft?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await Context.PublicAnnouncementDrafts
            .FirstOrDefaultAsync(x => x.PublicAnnouncementDraftKey == entityKey, cancellationToken);
    }

    public override async Task<IEnumerable<PublicAnnouncementDraft>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.PublicAnnouncementDrafts
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PublicAnnouncementDraft>> GetByStatusAsync(
        PublicAnnouncementStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = Context.PublicAnnouncementDrafts.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicAnnouncementDraft?> GetActiveBySourceAsync(
        PublicAnnouncementSourceType sourceType,
        string sourceId,
        Guid? exceptDraftKey = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSourceId = sourceId.Trim();
        var query = Context.PublicAnnouncementDrafts
            .Where(x => x.SourceType == sourceType
                && x.SourceId == normalizedSourceId
                && x.Status != PublicAnnouncementStatus.Deleted
                && x.Status != PublicAnnouncementStatus.Rejected);

        if (exceptDraftKey.HasValue)
        {
            query = query.Where(x => x.PublicAnnouncementDraftKey != exceptDraftKey.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await Context.PublicAnnouncementDrafts
            .AnyAsync(x => x.PublicAnnouncementDraftKey == entityKey, cancellationToken);
    }

}
