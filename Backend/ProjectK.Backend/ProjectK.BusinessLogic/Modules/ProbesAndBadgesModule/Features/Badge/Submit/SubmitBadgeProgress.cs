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
    private readonly IMemberDirectory _members;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDomainEventPublisher _events;

    public SubmitBadgeProgressHandler(
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

    public async Task<ServiceResult<BadgeProgressResponse>> Handle(SubmitBadgeProgress request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BadgeId))
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.BadRequest);
        }

        var member = await _members.FindAsync(request.MemberKey, cancellationToken);
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
        var actor = await ProgressActorResolver.ResolveAsync(_currentUserContext, _members, cancellationToken);

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
                ActorName = actor.Name,
                ActorRole = actor.Role,
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
                ActorName = actor.Name,
                ActorRole = actor.Role,
                OccurredAtUtc = now,
                Note = request.Note
            });

        }

        var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (changes <= 0)
        {
            return new ServiceResult<BadgeProgressResponse>(ResultType.InternalServerError);
        }

        await PublishSubmittedAsync(member, progress, cancellationToken);

        return new ServiceResult<BadgeProgressResponse>(ResultType.Success, BadgeProgressResponse.FromEntity(progress));
    }

    private async Task PublishSubmittedAsync(
        MemberSummary member,
        BadgeProgress progress,
        CancellationToken cancellationToken)
    {
        await _events.PublishAsync(
            new BadgeProgressSubmitted(
                progress.BadgeProgressKey,
                progress.BadgeId,
                member.MemberKey,
                member.FullName,
                member.KurinKey,
                member.GroupKey,
                _currentUserContext.UserId),
            cancellationToken);
    }
}
