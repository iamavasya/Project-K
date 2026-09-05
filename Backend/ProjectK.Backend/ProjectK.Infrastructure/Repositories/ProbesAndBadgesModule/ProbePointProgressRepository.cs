using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.ProbesAndBadgesModule;

public class ProbePointProgressRepository : BaseEntityRepository<ProbePointProgress>, IProbePointProgressRepository
{

    public ProbePointProgressRepository(AppDbContext context) : base(context)
        {
        }

    public override async Task<ProbePointProgress?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await Context.ProbePointProgresses
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ProbePointProgressKey == entityKey, cancellationToken);
    }

    public async Task<ProbePointProgress?> GetByMemberProbePointAsync(
        Guid memberKey,
        string probeId,
        string pointId,
        CancellationToken cancellationToken = default)
    {
        return await Context.ProbePointProgresses
            .AsTracking()
            .FirstOrDefaultAsync(
                x => x.MemberKey == memberKey && x.ProbeId == probeId && x.PointId == pointId,
                cancellationToken);
    }

    public async Task<IEnumerable<ProbePointProgress>> GetByMemberAndProbeAsync(
        Guid memberKey,
        string probeId,
        CancellationToken cancellationToken = default)
    {
        return await Context.ProbePointProgresses
            .Where(x => x.MemberKey == memberKey && x.ProbeId == probeId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public override Task<IEnumerable<ProbePointProgress>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetByMemberAndProbeAsync instead.");
    }

    public override async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await Context.ProbePointProgresses
            .AnyAsync(x => x.ProbePointProgressKey == entityKey, cancellationToken);
    }

    public override void Update(ProbePointProgress entity, CancellationToken cancellationToken = default) => MarkModified(entity);
}
