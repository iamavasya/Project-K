using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.BusinessLogic.Services.Audit;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Audit;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Project;

/// <summary>What the person writes into their third-probe file; the confirmations are not here.</summary>
public sealed record ThirdProbeFileInput(
    DateOnly? OpenedOn,
    string? Mentors,
    DateOnly? ConferenceOn,
    string? ConferenceWith,
    ThirdProbeCampKind? CampKind,
    string? CampName,
    DateOnly? CampFrom,
    DateOnly? CampTo,
    string? PhysicalBadgeId,
    string? Resume);

/// <summary>
/// Writes the third-probe file. A confirmed conference or camp whose details change loses its
/// confirmation: what was confirmed is no longer what is written.
/// </summary>
public sealed record SaveThirdProbeFileCommand(Guid MemberKey, string ProbeId, ThirdProbeFileInput Input)
    : IRequest<ServiceResult<ThirdProbeResponse>>;

public sealed class SaveThirdProbeFileCommandHandler : IRequestHandler<SaveThirdProbeFileCommand, ServiceResult<ThirdProbeResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProbeProjectAccess _access;
    private readonly AuditActorResolver _actors;
    private readonly IKurinAudit _audit;
    private readonly IProbesCatalogService _probes;
    private readonly IThirdProbeReader _reader;

    public SaveThirdProbeFileCommandHandler(
        IUnitOfWork unitOfWork,
        IProbeProjectAccess access,
        AuditActorResolver actors,
        IKurinAudit audit,
        IProbesCatalogService probes,
        IThirdProbeReader reader)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _actors = actors;
        _audit = audit;
        _probes = probes;
        _reader = reader;
    }

    public async Task<ServiceResult<ThirdProbeResponse>> Handle(SaveThirdProbeFileCommand request, CancellationToken cancellationToken)
    {
        var probeId = request.ProbeId.Trim();
        if (await ProbeProjectGuard.RefuseAsync<ThirdProbeResponse>(_access, request.MemberKey, probeId, cancellationToken) is { } refused)
        {
            return refused;
        }

        var input = request.Input;
        var mentors = Optional(input.Mentors);
        var conferenceWith = Optional(input.ConferenceWith);
        var campName = Optional(input.CampName);
        var physicalBadgeId = Optional(input.PhysicalBadgeId);
        var resume = input.Resume?.Trim() ?? string.Empty;

        if (Invalid(input, mentors, conferenceWith, campName, resume) is { } invalid)
        {
            return ProbeProjectGuard.Refuse<ThirdProbeResponse>(invalid.Code, invalid.Message);
        }

        if (physicalBadgeId is not null && !await HoldsAsync(request.MemberKey, physicalBadgeId, cancellationToken))
        {
            return ProbeProjectGuard.Refuse<ThirdProbeResponse>("BadgeNotHeld", "The вмілість must be one the person holds, confirmed.");
        }

        var file = await _unitOfWork.ProbeProjects.GetFileAsync(request.MemberKey, probeId, cancellationToken);
        if (file is null)
        {
            file = new ThirdProbeFile { MemberKey = request.MemberKey, ProbeId = probeId };
            _unitOfWork.ProbeProjects.AddFile(file);
        }

        if (file.ConferenceOn != input.ConferenceOn || file.ConferenceWith != conferenceWith)
        {
            ClearConference(file);
        }

        if (file.CampKind != input.CampKind || file.CampName != campName || file.CampFrom != input.CampFrom || file.CampTo != input.CampTo)
        {
            ClearCamp(file);
        }

        var actor = await _actors.ResolveAsync(cancellationToken);
        file.OpenedOn = input.OpenedOn;
        file.Mentors = mentors;
        file.ConferenceOn = input.ConferenceOn;
        file.ConferenceWith = conferenceWith;
        file.CampKind = input.CampKind;
        file.CampName = campName;
        file.CampFrom = input.CampFrom;
        file.CampTo = input.CampTo;
        file.PhysicalBadgeId = physicalBadgeId;
        file.Resume = resume;
        file.UpdatedDate = DateTime.UtcNow;
        file.UpdatedByUserKey = actor.UserKey;
        file.UpdatedByName = actor.Name;

        await _audit.RecordAsync(
            new AuditRecord(
                AuditArea.Probe,
                AuditActions.Probe.FileEdited,
                file.ThirdProbeFileKey,
                ProgressAuditLabels.Probe(_probes, probeId),
                request.MemberKey),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ServiceResult<ThirdProbeResponse>(ResultType.Success, await _reader.ReadAsync(request.MemberKey, probeId, cancellationToken));
    }

    internal static void ClearConference(ThirdProbeFile file)
    {
        file.ConferenceConfirmedAtUtc = null;
        file.ConferenceConfirmedByUserKey = null;
        file.ConferenceConfirmedByName = null;
    }

    internal static void ClearCamp(ThirdProbeFile file)
    {
        file.CampConfirmedAtUtc = null;
        file.CampConfirmedByUserKey = null;
        file.CampConfirmedByName = null;
    }

    private async Task<bool> HoldsAsync(Guid memberKey, string badgeId, CancellationToken cancellationToken)
    {
        var progress = await _unitOfWork.BadgeProgresses.GetByMemberAndBadgeIdAsync(memberKey, badgeId, cancellationToken);
        return progress?.Status == BadgeProgressStatus.Confirmed;
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static (string Code, string Message)? Invalid(
        ThirdProbeFileInput input,
        string? mentors,
        string? conferenceWith,
        string? campName,
        string resume)
    {
        const int shortText = ProbeProjectRules.MaxShortTextLength;
        if (mentors?.Length > ProbeProjectRules.MaxMentorsLength || conferenceWith?.Length > shortText || campName?.Length > shortText)
        {
            return ("FieldTooLong", $"Short fields hold at most {shortText} characters, mentors {ProbeProjectRules.MaxMentorsLength}.");
        }

        if (resume.Length > ProbeProjectRules.MaxResumeLength)
        {
            return ("DescriptionTooLong", $"The résumé holds at most {ProbeProjectRules.MaxResumeLength} characters.");
        }

        // The résumé carries no pictures: there is nowhere to upload them to.
        if (ProbeProjectMarkdown.ImageKeys(resume) is not { Count: 0 })
        {
            return ("ForeignImage", "The résumé holds text only.");
        }

        if (input.CampKind is { } kind && !Enum.IsDefined(kind))
        {
            return ("UnknownCampKind", "Unknown camp kind.");
        }

        if (input.CampFrom is { } from && input.CampTo is { } to && to < from)
        {
            return ("EndsBeforeStart", "A camp cannot end before it starts.");
        }

        return null;
    }
}
