using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.UpdateStatus;

public sealed class UpdateProbeProgressStatus : IRequest<ServiceResult<ProbeProgressResponse>>
{
    public UpdateProbeProgressStatus(Guid memberKey, string probeId, ProbeProgressStatus status, string? note)
    {
        MemberKey = memberKey;
        ProbeId = probeId;
        Status = status;
        Note = note;
    }

    public Guid MemberKey { get; }
    public string ProbeId { get; }
    public ProbeProgressStatus Status { get; }
    public string? Note { get; }
}

public sealed class UpdateProbeProgressStatusHandler : IRequestHandler<UpdateProbeProgressStatus, ServiceResult<ProbeProgressResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateProbeProgressStatusHandler(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<ServiceResult<ProbeProgressResponse>> Handle(UpdateProbeProgressStatus request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProbeId) || request.Status == ProbeProgressStatus.NotStarted)
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.BadRequest);
        }

        var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
        if (member is null)
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.NotFound);
        }

        var progress = await _unitOfWork.ProbeProgresses
            .GetByMemberAndProbeIdAsync(request.MemberKey, request.ProbeId, cancellationToken);

        var isNew = false;
        if (progress is null)
        {
            progress = new ProbeProgress
            {
                MemberKey = request.MemberKey,
                KurinKey = member.KurinKey,
                ProbeId = request.ProbeId.Trim(),
                Status = ProbeProgressStatus.NotStarted
            };

            _unitOfWork.ProbeProgresses.Create(progress, cancellationToken);
            isNew = true;
        }

        var currentStatus = progress.Status;
        if (currentStatus == request.Status)
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.Success, ProbeProgressResponse.FromEntity(progress));
        }

        if (!IsTransitionAllowed(currentStatus, request.Status))
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.Conflict);
        }

        var now = DateTime.UtcNow;
        var actor = ProgressActorResolver.Resolve(_currentUserContext);

        progress.Status = request.Status;

        if (request.Status == ProbeProgressStatus.Completed)
        {
            progress.CompletedAtUtc = now;
            progress.CompletedByUserKey = actor.UserKey;
            progress.CompletedByName = actor.ActorName;
            progress.CompletedByRole = actor.ActorRole;
        }

        if (request.Status == ProbeProgressStatus.Verified)
        {
            progress.VerifiedAtUtc = now;
            progress.VerifiedByUserKey = actor.UserKey;
            progress.VerifiedByName = actor.ActorName;
            progress.VerifiedByRole = actor.ActorRole;
        }

        progress.AuditEvents.Add(new ProbeProgressAuditEvent
        {
            FromStatus = currentStatus,
            ToStatus = request.Status,
            Action = request.Status.ToString(),
            ActorUserKey = actor.UserKey,
            ActorName = actor.ActorName,
            ActorRole = actor.ActorRole,
            OccurredAtUtc = now,
            Note = request.Note
        });

        if (!isNew)
        {
            _unitOfWork.ProbeProgresses.Update(progress, cancellationToken);
        }

        var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (changes <= 0)
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.InternalServerError);
        }

        return new ServiceResult<ProbeProgressResponse>(ResultType.Success, ProbeProgressResponse.FromEntity(progress));
    }

    private static bool IsTransitionAllowed(ProbeProgressStatus current, ProbeProgressStatus target)
    {
        return current switch
        {
            ProbeProgressStatus.NotStarted => target is ProbeProgressStatus.InProgress or ProbeProgressStatus.Completed,
            ProbeProgressStatus.InProgress => target == ProbeProgressStatus.Completed,
            ProbeProgressStatus.Completed => target == ProbeProgressStatus.Verified,
            ProbeProgressStatus.Verified => false,
            _ => false
        };
    }
}
