using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories;

public class BadgeProgressRepository : IBadgeProgressRepository
{
    private readonly AppDbContext _context;

    public BadgeProgressRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Create(BadgeProgress entity, CancellationToken cancellationToken = default)
    {
        _context.BadgeProgresses.Add(entity);
    }

    public void Delete(BadgeProgress entity, CancellationToken cancellationToken = default)
    {
        _context.BadgeProgresses.Remove(entity);
    }

    public async Task<BadgeProgress?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await _context.BadgeProgresses
            .Include(x => x.AuditEvents)
            .FirstOrDefaultAsync(x => x.BadgeProgressKey == entityKey, cancellationToken);
    }

    public async Task<BadgeProgress?> GetByMemberAndBadgeIdAsync(Guid memberKey, string badgeId, CancellationToken cancellationToken = default)
    {
        return await _context.BadgeProgresses
            .Include(x => x.AuditEvents)
            .FirstOrDefaultAsync(
                x => x.MemberKey == memberKey && x.BadgeId == badgeId,
                cancellationToken);
    }

    public async Task<IEnumerable<BadgeProgress>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
    {
        return await _context.BadgeProgresses
            .Where(x => x.MemberKey == memberKey)
            .Include(x => x.AuditEvents)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<IEnumerable<BadgeProgress>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use GetByMemberKeyAsync instead.");
    }

    public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
    {
        return await _context.BadgeProgresses
            .AnyAsync(x => x.BadgeProgressKey == entityKey, cancellationToken);
    }

    public void Update(BadgeProgress entity, CancellationToken cancellationToken = default)
    {
        _context.BadgeProgresses.Update(entity);
    }
}
