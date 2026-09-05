using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.ProbesAndBadgesModule;

public class ProbeProgressRepository : BaseEntityRepository<ProbeProgress>, IProbeProgressRepository
{

    public ProbeProgressRepository(AppDbContext context) : base(context)
        {
        }

    public override async Task<ProbeProgress?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await Context.ProbeProgresses
            .AsTracking()
            .Include(x => x.AuditEvents)
            .FirstOrDefaultAsync(x => x.ProbeProgressKey == entityKey, cancellationToken);
    }

    public async Task<ProbeProgress?> GetByMemberAndProbeIdAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default)
    {
        return await Context.ProbeProgresses
            .AsTracking()
            .FirstOrDefaultAsync(
                x => x.MemberKey == memberKey && x.ProbeId == probeId,
                cancellationToken);
    }

    public async Task<ProbeProgress?> GetByMemberAndProbeIdWithAuditAsync(
        Guid memberKey,
        string probeId,
        CancellationToken cancellationToken = default)
    {
        return await Context.ProbeProgresses
            .AsTracking()
            .Include(x => x.AuditEvents)
            .FirstOrDefaultAsync(
                x => x.MemberKey == memberKey && x.ProbeId == probeId,
                cancellationToken);
    }

    public async Task<IEnumerable<ProbeProgress>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
    {
        return await Context.ProbeProgresses
            .Where(x => x.MemberKey == memberKey)
            .Include(x => x.AuditEvents)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public override Task<IEnumerable<ProbeProgress>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetByMemberKeyAsync instead.");
    }

    public override async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await Context.ProbeProgresses
            .AnyAsync(x => x.ProbeProgressKey == entityKey, cancellationToken);
    }

    public override void Update(ProbeProgress entity, CancellationToken cancellationToken = default) => MarkModified(entity);
}
