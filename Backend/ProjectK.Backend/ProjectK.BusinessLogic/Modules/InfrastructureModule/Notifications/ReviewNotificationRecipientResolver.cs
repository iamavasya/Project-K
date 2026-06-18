using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
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
        var managerUserKeys = (await _unitOfWork.Members
                .GetMentorCandidatesLookupAsync(kurinKey, cancellationToken))
            .Where(candidate =>
                candidate.UserKey.HasValue
                && string.Equals(
                    candidate.UserRole,
                    UserRole.Manager.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            .Select(candidate => candidate.UserKey!.Value);

        var mentorUserKeys = Enumerable.Empty<Guid>();
        if (groupKey.HasValue)
        {
            var assignments = await _unitOfWork.MentorAssignments
                .GetByGroupKeyAsync(groupKey.Value, cancellationToken);
            mentorUserKeys = assignments
                .Where(assignment => assignment.RevokedAtUtc is null)
                .Select(assignment => assignment.MentorUserKey);
        }

        return managerUserKeys
            .Concat(mentorUserKeys)
            .Where(userKey => userKey != excludedUserKey)
            .Distinct()
            .ToList();
    }
}
