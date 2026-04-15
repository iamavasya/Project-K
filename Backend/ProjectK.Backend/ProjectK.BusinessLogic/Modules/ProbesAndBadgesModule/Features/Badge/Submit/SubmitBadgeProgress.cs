using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Submit;

public sealed class SubmitBadgeProgress : IRequest<ServiceResult<BadgeProgressResponse>>
{
    public SubmitBadgeProgress(Guid memberKey, string badgeId, string? note)
    {
        MemberKey = memberKey;
        BadgeId = badgeId;
        Note = note;
    }

    public Guid MemberKey { get; }
    public string BadgeId { get; }
    public string? Note { get; }
}

public sealed class SubmitBadgeProgressHandler : IRequestHandler<SubmitBadgeProgress, ServiceResult<BadgeProgressResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public SubmitBadgeProgressHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<ServiceResult<BadgeProgressResponse>> Handle(SubmitBadgeProgress request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BadgeId))
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.BadRequest);
        }

        var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
        if (member is null)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.NotFound);
        }

        var progress = await _unitOfWork.BadgeProgresses
            .GetByMemberAndBadgeIdAsync(request.MemberKey, request.BadgeId, cancellationToken);

        if (progress?.Status == BadgeProgressStatus.Confirmed)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.Conflict);
        }

        if (progress?.Status == BadgeProgressStatus.Submitted)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.Success, BadgeProgressResponse.FromEntity(progress));
        }

        var now = DateTime.UtcNow;
        var actor = ProgressActorResolver.Resolve(_currentUserContext);

        if (progress is null)
        {
            progress = new BadgeProgress
            {
                MemberKey = request.MemberKey,
                KurinKey = member.KurinKey,
                BadgeId = request.BadgeId.Trim(),
                Status = BadgeProgressStatus.Submitted,
                SubmittedAtUtc = now,
                ReviewNote = request.Note
            };

            progress.AuditEvents.Add(new BadgeProgressAuditEvent
            {
                FromStatus = null,
                ToStatus = BadgeProgressStatus.Submitted,
                Action = "Submitted",
                ActorUserKey = actor.UserKey,
                ActorName = actor.ActorName,
                ActorRole = actor.ActorRole,
                OccurredAtUtc = now,
                Note = request.Note
            });

            _unitOfWork.BadgeProgresses.Create(progress, cancellationToken);
        }
        else
        {
            var previousStatus = progress.Status;

            progress.Status = BadgeProgressStatus.Submitted;
            progress.SubmittedAtUtc = now;
            progress.ReviewedAtUtc = null;
            progress.ReviewedByUserKey = null;
            progress.ReviewedByName = null;
            progress.ReviewedByRole = null;
            progress.ReviewNote = request.Note;

            progress.AuditEvents.Add(new BadgeProgressAuditEvent
            {
                FromStatus = previousStatus,
                ToStatus = BadgeProgressStatus.Submitted,
                Action = "Resubmitted",
                ActorUserKey = actor.UserKey,
                ActorName = actor.ActorName,
                ActorRole = actor.ActorRole,
                OccurredAtUtc = now,
                Note = request.Note
            });

            _unitOfWork.BadgeProgresses.Update(progress, cancellationToken);
        }

        var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (changes <= 0)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.InternalServerError);
        }

        return new ServiceResult<BadgeProgressResponse>(ResultType.Success, BadgeProgressResponse.FromEntity(progress));
    }
}
