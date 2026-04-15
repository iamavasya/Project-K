using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories;

public class ProbeProgressRepository : IProbeProgressRepository
{
    private readonly AppDbContext _context;

    public ProbeProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Create(ProbeProgress entity, CancellationToken cancellationToken = default)
    {
        _context.ProbeProgresses.Add(entity);
    }

    public void Delete(ProbeProgress entity, CancellationToken cancellationToken = default)
    {
        _context.ProbeProgresses.Remove(entity);
    }

    public async Task<ProbeProgress?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await _context.ProbeProgresses
            .Include(x => x.AuditEvents)
            .FirstOrDefaultAsync(x => x.ProbeProgressKey == entityKey, cancellationToken);
    }

    public async Task<ProbeProgress?> GetByMemberAndProbeIdAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default)
    {
        return await _context.ProbeProgresses
            .Include(x => x.AuditEvents)
            .FirstOrDefaultAsync(
                x => x.MemberKey == memberKey && x.ProbeId == probeId,
                cancellationToken);
    }

    public async Task<IEnumerable<ProbeProgress>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
    {
        return await _context.ProbeProgresses
            .Where(x => x.MemberKey == memberKey)
            .Include(x => x.AuditEvents)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<IEnumerable<ProbeProgress>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetByMemberKeyAsync instead.");
    }

    public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await _context.ProbeProgresses
            .AnyAsync(x => x.ProbeProgressKey == entityKey, cancellationToken);
    }

    public void Update(ProbeProgress entity, CancellationToken cancellationToken = default)
    {
        _context.ProbeProgresses.Update(entity);
    }
}
