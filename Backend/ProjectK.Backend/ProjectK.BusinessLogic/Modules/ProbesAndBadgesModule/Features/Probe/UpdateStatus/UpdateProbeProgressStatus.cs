using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
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
    private readonly IProbesCatalogService _probesCatalogService;

    public UpdateProbeProgressStatusHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext,
        IProbesCatalogService probesCatalogService)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
        _probesCatalogService = probesCatalogService;
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
        }

        var currentStatus = progress.Status;
        if (currentStatus == request.Status)
        {
            var pointSignatures = await GetPointSignatureResponsesAsync(request.MemberKey, request.ProbeId, cancellationToken);
            return new ServiceResult<ProbeProgressResponse>(
                ResultType.Success,
                ProbeProgressResponse.FromEntity(progress, pointSignatures));
        }

        if (!IsTransitionAllowed(currentStatus, request.Status))
        {
            var pointSignatures = await GetPointSignatureResponsesAsync(request.MemberKey, request.ProbeId, cancellationToken);
            return new ServiceResult<ProbeProgressResponse>(
                ResultType.Conflict,
                ProbeProgressResponse.FromEntity(progress, pointSignatures));
        }

        if (request.Status == ProbeProgressStatus.Completed
            && !await HasAllPointsSignedForCompletionAsync(request.MemberKey, request.ProbeId, cancellationToken))
        {
            var pointSignatures = await GetPointSignatureResponsesAsync(request.MemberKey, request.ProbeId, cancellationToken);
            return new ServiceResult<ProbeProgressResponse>(
                ResultType.Conflict,
                ProbeProgressResponse.FromEntity(progress, pointSignatures));
        }

        var now = DateTime.UtcNow;
        var actor = ProgressActorResolver.Resolve(_currentUserContext);
        var actorName = await ResolveActorDisplayNameAsync(actor.UserKey, cancellationToken);
        var auditAction = ResolveAuditAction(currentStatus, request.Status);

        progress.Status = request.Status;

        ApplyStatusSideEffects(progress, currentStatus, request.Status, now, actor.UserKey, actorName, actor.ActorRole);

        progress.AuditEvents.Add(new ProbeProgressAuditEvent
        {
            FromStatus = currentStatus,
            ToStatus = request.Status,
            Action = auditAction,
            ActorUserKey = actor.UserKey,
            ActorName = actorName,
            ActorRole = actor.ActorRole,
            OccurredAtUtc = now,
            Note = request.Note
        });

        var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (changes <= 0)
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.InternalServerError);
        }

        var updatedPointSignatures = await GetPointSignatureResponsesAsync(request.MemberKey, request.ProbeId, cancellationToken);
        return new ServiceResult<ProbeProgressResponse>(
            ResultType.Success,
            ProbeProgressResponse.FromEntity(progress, updatedPointSignatures));
    }

    private async Task<IReadOnlyCollection<ProbePointProgressResponse>> GetPointSignatureResponsesAsync(
        Guid memberKey,
        string probeId,
        CancellationToken cancellationToken)
    {
        var pointSignatures = await _unitOfWork.ProbePointProgresses
            .GetByMemberAndProbeAsync(memberKey, probeId.Trim(), cancellationToken);

        return pointSignatures
            .OrderBy(x => x.PointId)
            .Select(x => new ProbePointProgressResponse(
                x.ProbePointProgressKey,
                x.PointId,
                x.IsSigned,
                x.SignedAtUtc,
                x.SignedByUserKey,
                x.SignedByName,
                x.SignedByRole))
            .ToList();
    }

    private async Task<bool> HasAllPointsSignedForCompletionAsync(
        Guid memberKey,
        string probeId,
        CancellationToken cancellationToken)
    {
        var groupedProbe = _probesCatalogService.GetGroupedProbeById(probeId.Trim());
        if (groupedProbe is null || groupedProbe.PointsCount <= 0)
        {
            return true;
        }

        var pointSignatures = await _unitOfWork.ProbePointProgresses
            .GetByMemberAndProbeAsync(memberKey, probeId.Trim(), cancellationToken);

        var signedPointCount = pointSignatures
            .Where(x => x.IsSigned)
            .Select(x => x.PointId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return signedPointCount >= groupedProbe.PointsCount;
    }

    private static bool IsTransitionAllowed(ProbeProgressStatus current, ProbeProgressStatus target)
    {
        return current switch
        {
            ProbeProgressStatus.NotStarted => target is ProbeProgressStatus.InProgress or ProbeProgressStatus.Completed,
            ProbeProgressStatus.InProgress => target == ProbeProgressStatus.Completed,
            ProbeProgressStatus.Completed => target == ProbeProgressStatus.Verified,
            ProbeProgressStatus.Verified => target == ProbeProgressStatus.Completed,
            _ => false
        };
    }

    private async Task<string?> ResolveActorDisplayNameAsync(Guid? actorUserKey, CancellationToken cancellationToken)
    {
        if (actorUserKey is null)
        {
            return null;
        }

        var actorMember = await _unitOfWork.Members.GetByUserKeyAsync(actorUserKey.Value, cancellationToken);
        if (actorMember is not null)
        {
            var fullName = $"{actorMember.FirstName} {actorMember.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            return $"member:{actorMember.MemberKey} / user:{actorUserKey.Value}";
        }

        return $"user:{actorUserKey.Value}";
    }

    private static string ResolveAuditAction(ProbeProgressStatus currentStatus, ProbeProgressStatus targetStatus)
    {
        if (targetStatus == ProbeProgressStatus.InProgress
            && (currentStatus == ProbeProgressStatus.Completed || currentStatus == ProbeProgressStatus.Verified))
        {
            return "Unsign";
        }

        if (targetStatus == ProbeProgressStatus.Completed && currentStatus == ProbeProgressStatus.Verified)
        {
            return "Unverify";
        }

        return targetStatus.ToString();
    }

    private static void ApplyStatusSideEffects(
        ProbeProgress progress,
        ProbeProgressStatus currentStatus,
        ProbeProgressStatus targetStatus,
        DateTime now,
        Guid? actorUserKey,
        string? actorName,
        string actorRole)
    {
        if (targetStatus == ProbeProgressStatus.Completed)
        {
            if (currentStatus == ProbeProgressStatus.Verified)
            {
                progress.VerifiedAtUtc = null;
                progress.VerifiedByUserKey = null;
                progress.VerifiedByName = null;
                progress.VerifiedByRole = null;
                return;
            }

            progress.CompletedAtUtc = now;
            progress.CompletedByUserKey = actorUserKey;
            progress.CompletedByName = actorName;
            progress.CompletedByRole = actorRole;
            return;
        }

        if (targetStatus == ProbeProgressStatus.Verified)
        {
            progress.VerifiedAtUtc = now;
            progress.VerifiedByUserKey = actorUserKey;
            progress.VerifiedByName = actorName;
            progress.VerifiedByRole = actorRole;
            return;
        }

        if (targetStatus == ProbeProgressStatus.InProgress)
        {
            progress.CompletedAtUtc = null;
            progress.CompletedByUserKey = null;
            progress.CompletedByName = null;
            progress.CompletedByRole = null;
            progress.VerifiedAtUtc = null;
            progress.VerifiedByUserKey = null;
            progress.VerifiedByName = null;
            progress.VerifiedByRole = null;
        }
    }
}
