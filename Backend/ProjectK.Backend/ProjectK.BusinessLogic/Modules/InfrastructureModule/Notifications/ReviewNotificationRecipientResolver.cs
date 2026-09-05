using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;

public sealed class ReviewNotificationRecipientResolver : IReviewNotificationRecipientResolver
{
    /// <summary>
    /// Offices whose grants let them act on any member in the kurin. Derived from the permission map
    /// rather than listed, because a hardcoded list here kept notifying Курінний and Гуртковий after
    /// the офіс model took their member rights away.
    /// </summary>
    private static readonly LeadershipRole[] KurinWideReviewerOffices = LeadershipOffices.All()
        .Where(office => RolePermissionMap.WidestScope(
            RolePermissionMap.Resolve(new[] { SystemRole.ForOffice(office.Type, office.Role) }),
            ResourceType.Member,
            ResourceAction.Update) == AccessScope.KurinWide)
        .Select(office => office.Role)
        .Distinct()
        .ToArray();

    private readonly IUnitOfWork _unitOfWork;

    public ReviewNotificationRecipientResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<Guid>> ResolveAsync(
        Guid kurinKey,
        Guid? groupKey,
        Guid? excludedUserKey,
        CancellationToken cancellationToken = default)
    {
        // Map member -> user once; reviewers are addressed as users.
        var memberToUser = (await _unitOfWork.Members.GetMentorCandidatesLookupAsync(kurinKey, cancellationToken))
            .Where(candidate => candidate.UserKey.HasValue)
            .GroupBy(candidate => candidate.MemberKey)
            .ToDictionary(group => group.Key, group => group.First().UserKey!.Value);

        var managerMemberKeys = await _unitOfWork.Leaderships
            .GetActiveOfficeMemberKeysAsync(KurinWideReviewerOffices, kurinKey: kurinKey, cancellationToken: cancellationToken);

        var reviewerUserKeys = new HashSet<Guid>(ResolveUsers(managerMemberKeys, memberToUser));

        // A Виховник reviews only where he leads, and that scoping comes from his mentor assignments.
        if (groupKey.HasValue)
        {
            var assignments = await _unitOfWork.MentorAssignments
                .GetByGroupKeyAsync(groupKey.Value, cancellationToken);
            foreach (var userKey in assignments.Where(a => a.RevokedAtUtc is null).Select(a => a.MentorUserKey))
            {
                reviewerUserKeys.Add(userKey);
            }
        }

        if (excludedUserKey.HasValue)
        {
            reviewerUserKeys.Remove(excludedUserKey.Value);
        }

        return reviewerUserKeys.ToList();
    }

    private static IEnumerable<Guid> ResolveUsers(
        IEnumerable<Guid> memberKeys,
        IReadOnlyDictionary<Guid, Guid> memberToUser) =>
        memberKeys
            .Where(memberToUser.ContainsKey)
            .Select(memberKey => memberToUser[memberKey]);
}
