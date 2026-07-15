using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

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
    private readonly ICurrentUserContext _currentUserContext;
    private readonly INotificationService _notificationService;

    public ReviewBadgeProgressHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _notificationService = notificationService;
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
        var member = await _unitOfWork.Members.GetByKeyAsync(progress.MemberKey, cancellationToken);
        if (member?.UserKey is null)
        {
            return;
        }

        var wasRemoved = string.Equals(action, "RemovedConfirmed", StringComparison.Ordinal);
        var title = isApproved
            ? "Вмілість зараховано"
            : wasRemoved
                ? "Підтвердження вмілості скасовано"
                : "Вмілість потребує доопрацювання";
        var body = isApproved
            ? "Вашу вмілість зараховано."
            : wasRemoved
                ? "Раніше зараховану вмілість вилучено."
                : "Вашу вмілість не зараховано. Перегляньте зауваження та подайте її повторно.";

        await _notificationService.NotifyAsync(
            new NotificationRequest
            {
                RecipientUserKey = member.UserKey.Value,
                Type = AppNotificationType.MemberSkillReviewed,
                Severity = isApproved ? AppNotificationSeverity.Success : AppNotificationSeverity.Warn,
                Title = title,
                Body = body,
                EntityType = "BadgeProgress",
                EntityKey = progress.BadgeProgressKey,
                Route = $"/member/{progress.MemberKey}",
                ActorUserKey = _currentUserContext.UserId,
                DeduplicationKey = $"skill-review-result:{progress.MemberKey}:{progress.BadgeId}"
            },
            cancellationToken);
    }
}
