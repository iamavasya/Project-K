using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.InfrastructureModule
{
    public class ResourceScopeReader : IResourceScopeReader
    {
        private readonly AppDbContext _context;

        public ResourceScopeReader(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ResourceScope?> GetScopeAsync(
            ResourceType resourceType,
            Guid resourceKey,
            CancellationToken cancellationToken = default)
        {
            return resourceType switch
            {
                ResourceType.Member => await _context.Members
                    .Where(m => m.MemberKey == resourceKey)
                    .Select(m => new ResourceScope(m.KurinKey, m.GroupKey, m.UserKey))
                    .FirstOrDefaultAsync(cancellationToken),

                ResourceType.Group => await _context.Groups
                    .Where(g => g.GroupKey == resourceKey)
                    .Select(g => new ResourceScope(g.KurinKey, g.GroupKey, null))
                    .FirstOrDefaultAsync(cancellationToken),

                ResourceType.Kurin => await _context.Kurins
                    .Where(k => k.KurinKey == resourceKey)
                    .Select(k => new ResourceScope(k.KurinKey, null, null))
                    .FirstOrDefaultAsync(cancellationToken),

                ResourceType.PlanningSession => await _context.PlanningSessions
                    .Where(p => p.PlanningSessionKey == resourceKey)
                    .Select(p => new ResourceScope(p.KurinKey, null, null))
                    .FirstOrDefaultAsync(cancellationToken),

                ResourceType.Leadership => await GetLeadershipScopeAsync(resourceKey, cancellationToken),

                // Progress records carry the kurin already, but the group and owning user
                // come from the member the rules are actually about.
                ResourceType.ProbeProgress => await _context.ProbeProgresses
                    .Where(p => p.ProbeProgressKey == resourceKey)
                    .Join(_context.Members, p => p.MemberKey, m => m.MemberKey, (p, m) => m)
                    .Select(m => new ResourceScope(m.KurinKey, m.GroupKey, m.UserKey))
                    .FirstOrDefaultAsync(cancellationToken),

                ResourceType.BadgeProgress => await _context.BadgeProgresses
                    .Where(b => b.BadgeProgressKey == resourceKey)
                    .Join(_context.Members, b => b.MemberKey, m => m.MemberKey, (b, m) => m)
                    .Select(m => new ResourceScope(m.KurinKey, m.GroupKey, m.UserKey))
                    .FirstOrDefaultAsync(cancellationToken),

                _ => null
            };
        }

        public async Task<IReadOnlyCollection<Guid>> GetLedGroupKeysAsync(
            Guid userKey,
            Guid kurinKey,
            CancellationToken cancellationToken = default)
        {
            // Primary source: groups where the user currently holds a гуртковий-провід office.
            var officeGroups = await _context.LeadershipHistories
                .Where(h => h.EndDate == null)
                .Join(
                    _context.Leaderships.Where(l => l.Type == LeadershipType.Group && l.GroupKey != null),
                    h => h.LeadershipKey,
                    l => l.LeadershipKey,
                    (h, l) => new { h.MemberKey, GroupKey = l.GroupKey!.Value })
                .Join(
                    _context.Members.Where(m => m.UserKey == userKey && m.KurinKey == kurinKey),
                    x => x.MemberKey,
                    m => m.MemberKey,
                    (x, m) => x.GroupKey)
                .ToListAsync(cancellationToken);

            // Legacy source: explicit mentor assignments, kept until fully migrated to offices.
            var assigned = await _context.MentorAssignments
                .Where(a => a.MentorUserKey == userKey && a.RevokedAtUtc == null)
                .Select(a => a.GroupKey)
                .ToListAsync(cancellationToken);

            // Compatibility fallback: a group leader also covers the group they are a member of.
            var ownGroupKey = await _context.Members
                .Where(m => m.KurinKey == kurinKey && m.UserKey == userKey && m.GroupKey != null)
                .Select(m => m.GroupKey)
                .FirstOrDefaultAsync(cancellationToken);

            var ledGroups = officeGroups.Concat(assigned);
            if (ownGroupKey.HasValue)
            {
                ledGroups = ledGroups.Append(ownGroupKey.Value);
            }

            return ledGroups.Distinct().ToArray();
        }

        private async Task<ResourceScope?> GetLeadershipScopeAsync(Guid leadershipKey, CancellationToken cancellationToken)
        {
            var leadership = await _context.Leaderships
                .Where(l => l.LeadershipKey == leadershipKey)
                .Select(l => new { l.KurinKey, l.GroupKey })
                .FirstOrDefaultAsync(cancellationToken);

            if (leadership is null)
            {
                return null;
            }

            if (leadership.KurinKey.HasValue)
            {
                return new ResourceScope(leadership.KurinKey.Value, leadership.GroupKey, null);
            }

            if (!leadership.GroupKey.HasValue)
            {
                return null;
            }

            return await _context.Groups
                .Where(g => g.GroupKey == leadership.GroupKey.Value)
                .Select(g => new ResourceScope(g.KurinKey, g.GroupKey, null))
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
