using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories;

public class ProbePointProgressRepository : IProbePointProgressRepository
{
    private readonly AppDbContext _context;

    public ProbePointProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Create(ProbePointProgress entity, CancellationToken cancellationToken = default)
    {
        _context.ProbePointProgresses.Add(entity);
    }

    public void Delete(ProbePointProgress entity, CancellationToken cancellationToken = default)
    {
        _context.ProbePointProgresses.Remove(entity);
    }

    public async Task<ProbePointProgress?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await _context.ProbePointProgresses
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ProbePointProgressKey == entityKey, cancellationToken);
    }

    public async Task<ProbePointProgress?> GetByMemberProbePointAsync(
        Guid memberKey,
        string probeId,
        string pointId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ProbePointProgresses
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
        return await _context.ProbePointProgresses
            .Where(x => x.MemberKey == memberKey && x.ProbeId == probeId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<IEnumerable<ProbePointProgress>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetByMemberAndProbeAsync instead.");
    }

    public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await _context.ProbePointProgresses
            .AnyAsync(x => x.ProbePointProgressKey == entityKey, cancellationToken);
    }

    public void Update(ProbePointProgress entity, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            _context.ProbePointProgresses.Update(entity);
            return;
        }

        entry.State = EntityState.Modified;
    }
}
