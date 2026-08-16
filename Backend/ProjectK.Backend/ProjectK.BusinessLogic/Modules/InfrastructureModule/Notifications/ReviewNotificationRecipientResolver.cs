using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;

public sealed class ReviewNotificationRecipientResolver : IReviewNotificationRecipientResolver
{
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

        // Whole-kurin managers (Зв'язковий, Курінний) always review.
        var managerRoles = new[] { LeadershipRole.Zvyazkovyi, LeadershipRole.Kurinnuy };
        var managerMemberKeys = await _unitOfWork.Leaderships
            .GetActiveOfficeMemberKeysAsync(managerRoles, kurinKey: kurinKey, cancellationToken: cancellationToken);

        var reviewerUserKeys = new HashSet<Guid>(ResolveUsers(managerMemberKeys, memberToUser));

        // Group leaders (гуртковий office holders and legacy mentor assignments) review their group.
        if (groupKey.HasValue)
        {
            var groupLeaderMemberKeys = await _unitOfWork.Leaderships
                .GetActiveOfficeMemberKeysAsync(new[] { LeadershipRole.Hurtkoviy }, groupKey: groupKey.Value, cancellationToken: cancellationToken);
            foreach (var userKey in ResolveUsers(groupLeaderMemberKeys, memberToUser))
            {
                reviewerUserKeys.Add(userKey);
            }

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
