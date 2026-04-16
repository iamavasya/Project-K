using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
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

    public ReviewBadgeProgressHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
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

        if (progress.Status != BadgeProgressStatus.Submitted)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.Conflict);
        }

        var now = DateTime.UtcNow;
        var actor = ProgressActorResolver.Resolve(_currentUserContext);
        var targetStatus = request.IsApproved ? BadgeProgressStatus.Confirmed : BadgeProgressStatus.Rejected;

        progress.Status = targetStatus;
        progress.ReviewedAtUtc = now;
        progress.ReviewedByUserKey = actor.UserKey;
        progress.ReviewedByName = actor.ActorName;
        progress.ReviewedByRole = actor.ActorRole;
        progress.ReviewNote = request.Note;

        progress.AuditEvents.Add(new BadgeProgressAuditEvent
        {
            FromStatus = BadgeProgressStatus.Submitted,
            ToStatus = targetStatus,
            Action = request.IsApproved ? "Confirmed" : "Rejected",
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

        return new ServiceResult<BadgeProgressResponse>(ResultType.Success, BadgeProgressResponse.FromEntity(progress));
    }
}
