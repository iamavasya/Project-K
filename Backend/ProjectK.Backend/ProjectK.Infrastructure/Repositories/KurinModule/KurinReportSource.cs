using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
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

        var members = await _context.Members
            .AsNoTracking()
            .AsSplitQuery()
            .Where(member => member.KurinKey == kurinKey)
            .Include(member => member.PlastLevelHistory)
            .Include(member => member.ProbeProgresses)
            .Include(member => member.ProbePointProgresses)
            .Include(member => member.BadgeProgresses)
            .Include(member => member.MemberWarnings)
            .Include(member => member.MemberAwards)
            .Include(member => member.LeadershipHistories)
                .ThenInclude(history => history.Leadership)
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
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

        var roleRows = await (
                from userRole in _context.UserRoles.AsNoTracking()
                join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where userKeys.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);

        var rolesByUserKey = roleRows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => row.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .ToArray() as IReadOnlyList<string>);

        return new KurinReportSourceData(
            kurin,
            groups,
            mentorAssignments,
            members,
            (IReadOnlyDictionary<Guid, AppUser>)usersByKey,
            rolesByUserKey);
    }
}
