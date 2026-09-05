using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Events;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Dtos.InfrastructureModule;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Review;

public sealed class ReviewBadgeProgress : IRequest<ServiceResult<BadgeProgressResponse>>
{
    public ReviewBadgeProgress(Guid memberKey, string badgeId, bool isApproved, string? note)
    {
        MemberKey = memberKey;
        BadgeId = badgeId;
        IsApproved = isApproved;
        Note = note;
    }

    public Guid MemberKey { get; }
    public string BadgeId { get; }
    public bool IsApproved { get; }
    public string? Note { get; }
}

public sealed class ReviewBadgeProgressHandler : IRequestHandler<ReviewBadgeProgress, ServiceResult<BadgeProgressResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemberDirectory _members;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDomainEventPublisher _events;

    public ReviewBadgeProgressHandler(
        IUnitOfWork unitOfWork,
        IMemberDirectory members,
        ICurrentUserContext currentUserContext,
        IDomainEventPublisher events)
    {
        _unitOfWork = unitOfWork;
        _members = members;
        _currentUserContext = currentUserContext;
        _events = events;
    }

    public async Task<ServiceResult<BadgeProgressResponse>> Handle(ReviewBadgeProgress request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BadgeId))
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.BadRequest);
        }

        var progress = await _unitOfWork.BadgeProgresses
            .GetByMemberAndBadgeIdAsync(request.MemberKey, request.BadgeId, cancellationToken);

        if (progress is null)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.NotFound);
        }

        var fromStatus = progress.Status;
        var canReviewSubmitted = fromStatus == BadgeProgressStatus.Submitted;
        var canRemoveConfirmed = fromStatus == BadgeProgressStatus.Confirmed && !request.IsApproved;
        if (!canReviewSubmitted && !canRemoveConfirmed)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.Conflict);
        }

        var now = DateTime.UtcNow;
        var actor = ProgressActorResolver.Resolve(_currentUserContext);
        var targetStatus = request.IsApproved ? BadgeProgressStatus.Confirmed : BadgeProgressStatus.Rejected;
        string action;
        if (request.IsApproved)
        {
            action = "Confirmed";
        }
        else if (fromStatus == BadgeProgressStatus.Confirmed)
        {
            action = "RemovedConfirmed";
        }
        else
        {
            action = "Rejected";
        }

        progress.Status = targetStatus;
        progress.ReviewedAtUtc = now;
        progress.ReviewedByUserKey = actor.UserKey;
        progress.ReviewedByName = actor.ActorName;
        progress.ReviewedByRole = actor.ActorRole;
        progress.ReviewNote = request.Note;

        progress.AuditEvents.Add(new BadgeProgressAuditEvent
        {
            FromStatus = fromStatus,
            ToStatus = targetStatus,
            Action = action,
            ActorUserKey = actor.UserKey,
            ActorName = actor.ActorName,
            ActorRole = actor.ActorRole,
            OccurredAtUtc = now,
            Note = request.Note
        });

        var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (changes <= 0)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.InternalServerError);
        }

        await NotifyMemberOwnerAsync(progress, request.IsApproved, action, cancellationToken);

        return new ServiceResult<BadgeProgressResponse>(ResultType.Success, BadgeProgressResponse.FromEntity(progress));
    }

    private async Task NotifyMemberOwnerAsync(
        BadgeProgress progress,
        bool isApproved,
        string action,
        CancellationToken cancellationToken)
    {
        var ownerUserKey = await _members.FindAccountKeyAsync(progress.MemberKey, cancellationToken);
        if (ownerUserKey is null)
        {
            return;
        }

        await _events.PublishAsync(
            new BadgeProgressReviewed(
                progress.BadgeProgressKey,
                progress.BadgeId,
                progress.MemberKey,
                ownerUserKey.Value,
                isApproved,
                string.Equals(action, "RemovedConfirmed", StringComparison.Ordinal),
                _currentUserContext.UserId),
            cancellationToken);
    }
}
