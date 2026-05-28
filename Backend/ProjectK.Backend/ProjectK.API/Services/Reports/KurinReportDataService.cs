using Microsoft.EntityFrameworkCore;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.Services.BlobStorageService;
using Microsoft.Extensions.Configuration;

namespace ProjectK.API.Services.Reports;

public sealed class KurinReportDataService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserContext _currentUser;
    private readonly BlobStorageOptions _blobOptions;
    private readonly KurinReportMediaService _mediaService;
    private readonly IProbesCatalogService _probesCatalogService;
    private readonly IBadgesCatalogService _badgesCatalogService;
    private readonly IConfiguration _configuration;

    public KurinReportDataService(
        AppDbContext dbContext,
        ICurrentUserContext currentUser,
        BlobStorageOptions blobOptions,
        KurinReportMediaService mediaService,
        IProbesCatalogService probesCatalogService,
        IBadgesCatalogService badgesCatalogService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _blobOptions = blobOptions;
        _mediaService = mediaService;
        _probesCatalogService = probesCatalogService;
        _badgesCatalogService = badgesCatalogService;
        _configuration = configuration;
    }

    public async Task<KurinReportData?> BuildAsync(Guid kurinKey, CancellationToken cancellationToken)
    {
        var kurin = await _dbContext.Kurins
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.KurinKey == kurinKey, cancellationToken);

        if (kurin is null)
        {
            return null;
        }

        var groups = await _dbContext.Groups
            .AsNoTracking()
            .Where(group => group.KurinKey == kurinKey)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        var groupKeys = groups.Select(group => group.GroupKey).ToArray();

        var mentorAssignments = await _dbContext.MentorAssignments
            .AsNoTracking()
            .Where(assignment => groupKeys.Contains(assignment.GroupKey) && assignment.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var members = await _dbContext.Members
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
            .Concat(_currentUser.UserId is Guid userId ? [userId] : [])
            .Distinct()
            .ToArray();

        var usersByKey = await _dbContext.Users
            .AsNoTracking()
            .Where(user => userKeys.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var roleRows = await (
                from userRole in _dbContext.UserRoles.AsNoTracking()
                join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
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

        var groupNamesByKey = groups.ToDictionary(group => group.GroupKey, group => group.Name);
        var memberByUserKey = members
            .Where(member => member.UserKey.HasValue)
            .GroupBy(member => member.UserKey!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var reportMembers = new List<KurinReportMember>(members.Count);
        foreach (var member in members)
        {
            reportMembers.Add(await BuildMemberReportAsync(member, groupNamesByKey, rolesByUserKey, cancellationToken));
        }

        var reportMembersByKey = reportMembers.ToDictionary(member => member.MemberKey);

        var reportGroups = new List<KurinReportGroup>(groups.Count);
        foreach (var group in groups)
        {
            reportGroups.Add(new KurinReportGroup(
                group.GroupKey,
                group.Name,
                group.Description,
                BuildBlobUrl(group.SilhouetteBlobName),
                await _mediaService.TryDownloadAsync(group.SilhouetteBlobName, cancellationToken),
                ResolveMentorNames(group.GroupKey, mentorAssignments, memberByUserKey, usersByKey),
                members
                    .Where(member => member.GroupKey == group.GroupKey)
                    .OrderBy(member => member.LastName)
                    .ThenBy(member => member.FirstName)
                    .Select(member => new KurinReportGroupMember(
                        member.MemberKey,
                        BuildFullName(member),
                        member.Email,
                        member.PhoneNumber,
                        member.LatestPlastLevel))
                    .ToArray()));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var keyVolunteerKeys = members
            .Where(member =>
                member.UserKey.HasValue
                && rolesByUserKey.TryGetValue(member.UserKey.Value, out var roles)
                && roles.Any(role => role.Equals(UserRole.Manager.ToClaimValue(), StringComparison.OrdinalIgnoreCase)
                    || role.Equals(UserRole.Mentor.ToClaimValue(), StringComparison.OrdinalIgnoreCase)
                    || role.Equals(UserRole.Admin.ToClaimValue(), StringComparison.OrdinalIgnoreCase)))
            .Select(member => member.MemberKey)
            .Concat(members
                .SelectMany(member => member.LeadershipHistories)
                .Where(history =>
                    (history.EndDate is null || history.EndDate >= today)
                    && (history.Leadership.Type == LeadershipType.Kurin
                        || history.Leadership.Type == LeadershipType.KV))
                .Select(history => history.MemberKey))
            .Distinct()
            .ToArray();

        var keyVolunteers = keyVolunteerKeys
            .Where(reportMembersByKey.ContainsKey)
            .Select(key => reportMembersByKey[key])
            .OrderBy(member => member.FullName)
            .ToArray();

        return new KurinReportData(
            new KurinReportHeader(
                DateTime.UtcNow,
                ResolveCurrentUserName(usersByKey),
                ResolveCurrentUserEmail(usersByKey),
                ResolveReleaseInfo("Version"),
                ResolveReleaseInfo("Codename", "CodeName")),
            new KurinReportKurin(
                kurin.KurinKey,
                kurin.Number,
                kurin.Stanytsia,
                kurin.RegionOrCountry,
                kurin.NamedAfter,
                kurin.Description,
                kurin.IsZbtKurin,
                kurin.ZbtUserCap),
            reportGroups,
            keyVolunteers,
            reportMembers);
    }

    private async Task<KurinReportMember> BuildMemberReportAsync(
        Member member,
        IReadOnlyDictionary<Guid, string> groupNamesByKey,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> rolesByUserKey,
        CancellationToken cancellationToken)
    {
        var roles = member.UserKey is Guid userKey && rolesByUserKey.TryGetValue(userKey, out var userRoles)
            ? userRoles
            : [];

        return new KurinReportMember(
            member.MemberKey,
            member.UserKey,
            member.GroupKey,
            member.GroupKey is Guid groupKey && groupNamesByKey.TryGetValue(groupKey, out var groupName)
                ? groupName
                : null,
            BuildFullName(member),
            BuildInitials(member),
            member.Email,
            member.PhoneNumber,
            member.DateOfBirth,
            member.Address,
            member.School,
            BuildBlobUrl(member.ProfilePhotoBlobName),
            await _mediaService.TryDownloadAsync(member.ProfilePhotoBlobName, cancellationToken),
            member.LatestPlastLevel,
            roles,
            member.PlastLevelHistory
                .OrderByDescending(item => item.DateAchieved)
                .Select(item => new KurinReportPlastLevel(item.PlastLevel, item.DateAchieved))
                .ToArray(),
            member.ProbeProgresses
                .OrderBy(item => item.ProbeId)
                .Select(item =>
                {
                    var probe = _probesCatalogService.GetGroupedProbeById(item.ProbeId);
                    return new KurinReportProbe(
                        item.ProbeId,
                        ResolveProbeTitle(item.ProbeId, probe),
                        item.Status,
                        KurinReportTerminology.ProbeStatus(item.Status),
                        item.CompletedAtUtc,
                        item.CompletedByName,
                        item.VerifiedAtUtc,
                        item.VerifiedByName);
                })
                .ToArray(),
            member.ProbePointProgresses
                .Where(item => item.IsSigned)
                .OrderBy(item => item.ProbeId)
                .ThenBy(item => item.PointId)
                .Select(item =>
                {
                    var probe = _probesCatalogService.GetGroupedProbeById(item.ProbeId);
                    return new KurinReportProbePoint(
                        item.ProbeId,
                        ResolveProbeTitle(item.ProbeId, probe),
                        item.PointId,
                        ResolveProbePointLabel(item.PointId, probe),
                        item.SignedAtUtc,
                        item.SignedByName,
                        item.SignedByRole);
                })
                .ToArray(),
            member.BadgeProgresses
                .Where(item => item.Status == BadgeProgressStatus.Confirmed)
                .OrderBy(item => item.BadgeId)
                .Select(item =>
                {
                    var badge = _badgesCatalogService.GetBadgeById(item.BadgeId);
                    return new KurinReportBadge(
                        item.BadgeId,
                        badge?.Title ?? item.BadgeId,
                        item.Status,
                        KurinReportTerminology.BadgeStatus(item.Status),
                        item.ReviewedAtUtc,
                        item.ReviewedByName,
                        item.ReviewedByRole);
                })
                .ToArray(),
            member.MemberWarnings
                .Where(item => item.RevokedAtUtc == null && item.ExpiresAtUtc >= DateTime.UtcNow)
                .OrderByDescending(item => item.IssuedAtUtc)
                .Select(item => new KurinReportWarning(
                    item.Level,
                    KurinReportTerminology.WarningLevel(item.Level),
                    item.IssuedAtUtc,
                    item.ExpiresAtUtc,
                    item.RevokedAtUtc.HasValue))
                .ToArray(),
            member.MemberAwards
                .OrderByDescending(item => item.DateAcquired)
                .Select(item => new KurinReportAward(
                    item.Level,
                    KurinReportTerminology.AwardLevel(item.Level),
                    item.DateAcquired,
                    item.Note,
                    item.Status,
                    KurinReportTerminology.BadgeStatus(item.Status)))
                .ToArray(),
            member.LeadershipHistories
                .OrderByDescending(item => item.StartDate)
                .Select(item => new KurinReportLeadershipHistory(
                    item.Leadership.Type,
                    KurinReportTerminology.LeadershipType(item.Leadership.Type),
                    item.Role,
                    KurinReportTerminology.LeadershipRole(item.Role),
                    ResolveLeadershipScopeName(item.Leadership, groupNamesByKey),
                    item.StartDate,
                    item.EndDate))
                .ToArray());
    }

    private IReadOnlyList<string> ResolveMentorNames(
        Guid groupKey,
        IEnumerable<MentorAssignment> mentorAssignments,
        IReadOnlyDictionary<Guid, Member> memberByUserKey,
        IReadOnlyDictionary<Guid, Common.Entities.AuthModule.AppUser> usersByKey)
    {
        return mentorAssignments
            .Where(assignment => assignment.GroupKey == groupKey)
            .Select(assignment =>
            {
                if (memberByUserKey.TryGetValue(assignment.MentorUserKey, out var member))
                {
                    return BuildFullName(member);
                }

                if (usersByKey.TryGetValue(assignment.MentorUserKey, out var user))
                {
                    return $"{user.FirstName} {user.LastName}".Trim();
                }

                return assignment.MentorUserKey.ToString();
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToArray();
    }

    private string ResolveCurrentUserName(IReadOnlyDictionary<Guid, Common.Entities.AuthModule.AppUser> usersByKey)
    {
        if (_currentUser.UserId is Guid userKey && usersByKey.TryGetValue(userKey, out var user))
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? user.Email ?? user.UserName ?? user.Id.ToString() : fullName;
        }

        return "Unknown user";
    }

    private string? ResolveCurrentUserEmail(IReadOnlyDictionary<Guid, Common.Entities.AuthModule.AppUser> usersByKey)
    {
        return _currentUser.UserId is Guid userKey && usersByKey.TryGetValue(userKey, out var user)
            ? user.Email
            : null;
    }

    private string? BuildBlobUrl(string? blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_blobOptions.PublicBaseUrl))
        {
            return blobName;
        }

        return $"{_blobOptions.PublicBaseUrl.TrimEnd('/')}/{EncodeBlobPath(blobName)}";
    }

    private string ResolveReleaseInfo(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _configuration[$"ReleaseInfo:{key}"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "unknown";
    }

    private static string EncodeBlobPath(string blobName)
        => string.Join("/", blobName
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));

    private static string BuildFullName(Member member)
        => string.Join(" ", new[] { member.FirstName, member.MiddleName, member.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string BuildInitials(Member member)
        => string.Concat(new[] { member.FirstName, member.MiddleName, member.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => char.ToUpperInvariant(part![0])));

    private static string? ResolveLeadershipScopeName(
        Leadership leadership,
        IReadOnlyDictionary<Guid, string> groupNamesByKey)
    {
        if (!string.IsNullOrWhiteSpace(leadership.Name))
        {
            return leadership.Name;
        }

        return leadership.Type switch
        {
            LeadershipType.Group when leadership.GroupKey is Guid groupKey && groupNamesByKey.TryGetValue(groupKey, out var groupName) => groupName,
            LeadershipType.Group => leadership.GroupKey?.ToString(),
            LeadershipType.Kurin => "Kurin",
            LeadershipType.KV => "KV",
            _ => null
        };
    }

    private static string ResolveProbeTitle(string probeId, GroupedProbeResponse? probe)
        => string.IsNullOrWhiteSpace(probe?.Title) ? probeId : probe.Title;

    private static string ResolveProbePointLabel(string pointId, GroupedProbeResponse? probe)
    {
        if (probe?.Sections is null)
        {
            return pointId;
        }

        foreach (var section in probe.Sections)
        {
            var points = section.Points ?? [];
            var pointIndex = points
                .Select((point, index) => new { point, index })
                .FirstOrDefault(item => item.point.Id == pointId)
                ?.index ?? -1;
            if (pointIndex < 0)
            {
                continue;
            }

            var point = points[pointIndex];
            var pointCode = !string.IsNullOrWhiteSpace(section.Code)
                ? $"{section.Code}{pointIndex + 1}"
                : pointId;
            return string.IsNullOrWhiteSpace(point.Title)
                ? $"точка {pointCode}"
                : $"точка {pointCode} - {point.Title}";
        }

        return pointId;
    }
}
