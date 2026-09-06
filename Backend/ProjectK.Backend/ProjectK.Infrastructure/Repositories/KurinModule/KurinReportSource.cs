using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.KurinModule;

public sealed class KurinReportSource : IKurinReportSource
{
    private readonly AppDbContext _context;

    public KurinReportSource(AppDbContext context)
    {
        _context = context;
    }

    public async Task<KurinReportSourceData?> LoadAsync(
        Guid kurinKey,
        Guid? currentUserKey,
        CancellationToken cancellationToken = default)
    {
        var kurin = await _context.Kurins
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.KurinKey == kurinKey, cancellationToken);

        if (kurin is null)
        {
            return null;
        }

        var groups = await _context.Groups
            .AsNoTracking()
            .Where(group => group.KurinKey == kurinKey)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        var groupKeys = groups.Select(group => group.GroupKey).ToArray();

        var mentorAssignments = await _context.MentorAssignments
            .AsNoTracking()
            .Where(assignment => groupKeys.Contains(assignment.GroupKey) && assignment.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        // Who the report is about is decided by membership, and the membership is also what says
        // which гурток each of them is in — the person's own record no longer carries either.
        var memberships = await _context.Memberships
            .AsNoTracking()
            .Where(membership => membership.KurinKey == kurinKey && membership.LeftAtUtc == null)
            .ToListAsync(cancellationToken);

        var memberKeysHere = memberships.Select(membership => membership.MemberKey).ToArray();

        var members = await _context.Members
            .AsNoTracking()
            .AsSplitQuery()
            .Where(member => memberKeysHere.Contains(member.MemberKey))
            .Include(member => member.PlastLevelHistory)
            .Include(member => member.MemberWarnings)
            .Include(member => member.MemberAwards)
            .Include(member => member.LeadershipHistories)
                .ThenInclude(history => history.Leadership)
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToListAsync(cancellationToken);

        // Progress hangs off the member by key alone, so it is read on its own rather than joined.
        var memberKeys = members.Select(member => member.MemberKey).ToArray();

        var probeProgress = await _context.ProbeProgresses
            .AsNoTracking()
            .Where(progress => memberKeys.Contains(progress.MemberKey))
            .ToListAsync(cancellationToken);

        var probePointProgress = await _context.ProbePointProgresses
            .AsNoTracking()
            .Where(progress => memberKeys.Contains(progress.MemberKey))
            .ToListAsync(cancellationToken);

        var badgeProgress = await _context.BadgeProgresses
            .AsNoTracking()
            .Where(progress => memberKeys.Contains(progress.MemberKey))
            .ToListAsync(cancellationToken);

        var userKeys = members
            .Select(member => member.UserKey)
            .OfType<Guid>()
            .Concat(mentorAssignments.Select(assignment => assignment.MentorUserKey))
            .Concat(currentUserKey is Guid userId ? [userId] : [])
            .Distinct()
            .ToArray();

        var usersByKey = await _context.Users
            .AsNoTracking()
            .Where(user => userKeys.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        // The offices held here, not what the identity store says a person is anywhere — an office
        // belongs to a kurin, and this report is about one.
        var officeRows = await (
                from history in _context.LeadershipHistories.AsNoTracking()
                where history.EndDate == null && memberKeysHere.Contains(history.MemberKey)
                join office in _context.Leaderships.AsNoTracking()
                    on history.LeadershipKey equals office.LeadershipKey
                where office.EndDate == null
                    && (office.KurinKey == kurinKey
                        || (office.GroupKey != null && office.Group!.KurinKey == kurinKey))
                select new { history.MemberKey, office.Type, history.Role })
            .ToListAsync(cancellationToken);

        var userKeyByMemberKey = members
            .Where(member => member.UserKey.HasValue)
            .ToDictionary(member => member.MemberKey, member => member.UserKey!.Value);

        var rolesByUserKey = officeRows
            .Where(row => userKeyByMemberKey.ContainsKey(row.MemberKey))
            .GroupBy(row => userKeyByMemberKey[row.MemberKey])
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => SystemRole.ForOffice(row.Type, row.Role))
                    .Append(SystemRole.Member)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .ToArray() as IReadOnlyList<string>);

        return new KurinReportSourceData(
            kurin,
            groups,
            mentorAssignments,
            members,
            (IReadOnlyDictionary<Guid, AppUser>)usersByKey,
            rolesByUserKey,
            GroupByMember(probeProgress, progress => progress.MemberKey),
            GroupByMember(probePointProgress, progress => progress.MemberKey),
            GroupByMember(badgeProgress, progress => progress.MemberKey),
            memberships.ToDictionary(membership => membership.MemberKey));
    }

    private static IReadOnlyDictionary<Guid, IReadOnlyList<T>> GroupByMember<T>(
        IEnumerable<T> rows,
        Func<T, Guid> memberKeyOf)
    {
        return rows
            .GroupBy(memberKeyOf)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<T>)group.ToArray());
    }
}
