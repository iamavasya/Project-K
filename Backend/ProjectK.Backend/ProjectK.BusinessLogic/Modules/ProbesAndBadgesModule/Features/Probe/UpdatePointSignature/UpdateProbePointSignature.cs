using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.UpdatePointSignature;

public sealed class UpdateProbePointSignature : IRequest<ServiceResult<ProbeProgressResponse>>
{
    public UpdateProbePointSignature(Guid memberKey, string probeId, string pointId, bool isSigned, string? note)
    {
        MemberKey = memberKey;
        ProbeId = probeId;
        PointId = pointId;
        IsSigned = isSigned;
        Note = note;
    }

    public Guid MemberKey { get; }
    public string ProbeId { get; }
    public string PointId { get; }
    public bool IsSigned { get; }
    public string? Note { get; }
}

public sealed class UpdateProbePointSignatureHandler : IRequestHandler<UpdateProbePointSignature, ServiceResult<ProbeProgressResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateProbePointSignatureHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUserContext)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<ServiceResult<ProbeProgressResponse>> Handle(UpdateProbePointSignature request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProbeId) || string.IsNullOrWhiteSpace(request.PointId))
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.BadRequest);
        }

        var normalizedProbeId = request.ProbeId.Trim();
        var normalizedPointId = request.PointId.Trim();

        var member = await _unitOfWork.Members.GetByKeyAsync(request.MemberKey, cancellationToken);
        if (member is null)
        {
            return new ServiceResult<ProbeProgressResponse>(ResultType.NotFound);
        }

        var pointProgress = await _unitOfWork.ProbePointProgresses
            .GetByMemberProbePointAsync(request.MemberKey, normalizedProbeId, normalizedPointId, cancellationToken);

        var probeProgress = await _unitOfWork.ProbeProgresses
            .GetByMemberAndProbeIdAsync(request.MemberKey, normalizedProbeId, cancellationToken);

        var actor = ProgressActorResolver.Resolve(_currentUserContext);
        var actorName = await ResolveActorDisplayNameAsync(actor.UserKey, cancellationToken);
        var now = DateTime.UtcNow;
        var hasChanges = false;
        var pointWasActuallyUnsigned = false;

        if (request.IsSigned)
        {
            if (pointProgress is null)
            {
                pointProgress = new ProbePointProgress
                {
                    MemberKey = request.MemberKey,
                    KurinKey = member.KurinKey,
                    ProbeId = normalizedProbeId,
                    PointId = normalizedPointId,
                    IsSigned = true,
                    SignedAtUtc = now,
                    SignedByUserKey = actor.UserKey,
                    SignedByName = actorName,
                    SignedByRole = actor.ActorRole
                };
                _unitOfWork.ProbePointProgresses.Create(pointProgress, cancellationToken);
                hasChanges = true;
            }
            else if (!pointProgress.IsSigned)
            {
                pointProgress.IsSigned = true;
                pointProgress.SignedAtUtc = now;
                pointProgress.SignedByUserKey = actor.UserKey;
                pointProgress.SignedByName = actorName;
                pointProgress.SignedByRole = actor.ActorRole;
                _unitOfWork.ProbePointProgresses.Update(pointProgress, cancellationToken);
                hasChanges = true;
            }
        }
        else
        {
            if (pointProgress is not null && pointProgress.IsSigned)
            {
                pointProgress.IsSigned = false;
                pointProgress.SignedAtUtc = null;
                pointProgress.SignedByUserKey = null;
                pointProgress.SignedByName = null;
                pointProgress.SignedByRole = null;
                _unitOfWork.ProbePointProgresses.Update(pointProgress, cancellationToken);
                hasChanges = true;
                pointWasActuallyUnsigned = true;
            }
        }

        if (probeProgress is null)
        {
            if (request.IsSigned)
            {
                probeProgress = new ProbeProgress
                {
                    MemberKey = request.MemberKey,
                    KurinKey = member.KurinKey,
                    ProbeId = normalizedProbeId,
                    Status = ProbeProgressStatus.InProgress
                };
                _unitOfWork.ProbeProgresses.Create(probeProgress, cancellationToken);
                hasChanges = true;
            }
        }
        else if (!request.IsSigned
            && pointWasActuallyUnsigned
            && (probeProgress.Status == ProbeProgressStatus.Completed || probeProgress.Status == ProbeProgressStatus.Verified))
        {
            var previousStatus = probeProgress.Status;
            probeProgress.Status = ProbeProgressStatus.InProgress;
            probeProgress.CompletedAtUtc = null;
            probeProgress.CompletedByUserKey = null;
            probeProgress.CompletedByName = null;
            probeProgress.CompletedByRole = null;
            probeProgress.VerifiedAtUtc = null;
            probeProgress.VerifiedByUserKey = null;
            probeProgress.VerifiedByName = null;
            probeProgress.VerifiedByRole = null;
            hasChanges = true;
        }
        else if (!request.IsSigned
            && !pointWasActuallyUnsigned
            && (probeProgress.Status == ProbeProgressStatus.Completed || probeProgress.Status == ProbeProgressStatus.Verified))
        {
        }
        else if (request.IsSigned && probeProgress.Status == ProbeProgressStatus.NotStarted)
        {
            probeProgress.Status = ProbeProgressStatus.InProgress;
            hasChanges = true;
        }

        if (hasChanges)
        {
            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (changes <= 0)
            {
                return new ServiceResult<ProbeProgressResponse>(ResultType.InternalServerError);
            }
        }

        var latestPointSignatures = await BuildPointSignatureResponsesAsync(request.MemberKey, normalizedProbeId, cancellationToken);

        if (probeProgress is null)
        {
            var notStarted = ProbeProgressResponse.CreateNotStarted(
                request.MemberKey,
                member.KurinKey,
                normalizedProbeId,
                latestPointSignatures);
            return new ServiceResult<ProbeProgressResponse>(ResultType.Success, notStarted);
        }

        return new ServiceResult<ProbeProgressResponse>(
            ResultType.Success,
            ProbeProgressResponse.FromEntity(probeProgress, latestPointSignatures));
    }

    private async Task<IReadOnlyCollection<ProbePointProgressResponse>> BuildPointSignatureResponsesAsync(
        Guid memberKey,
        string probeId,
        CancellationToken cancellationToken)
    {
        var pointSignatures = await _unitOfWork.ProbePointProgresses
            .GetByMemberAndProbeAsync(memberKey, probeId, cancellationToken);

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
}
