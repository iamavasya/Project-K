using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;

/// <param name="Goal">Markdown, like <paramref name="Plan"/> and <paramref name="Results"/>; pictures are
/// <c>project-image:{key}</c>, their links in <paramref name="Images"/>.</param>
public sealed record ProbeProjectResponse(
    Guid ProjectKey,
    string Title,
    ProbeProjectArea? Area,
    IReadOnlyList<ProbeProjectReach> Reach,
    bool IsShared,
    string? SharedWith,
    bool IsSpare,
    DateOnly? StartsOn,
    DateOnly? EndsOn,
    string? Location,
    string? EventLeads,
    string? CoordinatorName,
    string? CoordinatorContact,
    string Goal,
    string Plan,
    string Results,
    DateTime? ApprovedAtUtc,
    string? ApprovedByName,
    DateTime? CompletedAtUtc,
    string? CompletedByName,
    string? CoordinatorComment,
    DateTime UpdatedAtUtc,
    string? UpdatedByName,
    IReadOnlyList<ProbeProjectImageResponse> Images);

/// <param name="Url">A signed link that expires; never store it, store the key.</param>
public sealed record ProbeProjectImageResponse(Guid ImageKey, string Url);

/// <param name="Url">A signed link that expires.</param>
public sealed record ThirdProbeCertificateResponse(string Url, string ContentType, string? FileName);

/// <param name="OpenedOn">The date written down, or else the day the previous probe was closed.</param>
/// <param name="OpenedOnIsDerived">True when <paramref name="OpenedOn"/> was not written down but taken
/// from the previous probe.</param>
public sealed record ThirdProbeFileResponse(
    DateOnly? OpenedOn,
    bool OpenedOnIsDerived,
    string? Mentors,
    DateOnly? ConferenceOn,
    string? ConferenceWith,
    DateTime? ConferenceConfirmedAtUtc,
    string? ConferenceConfirmedByName,
    ThirdProbeCampKind? CampKind,
    string? CampName,
    DateOnly? CampFrom,
    DateOnly? CampTo,
    ThirdProbeCertificateResponse? CampCertificate,
    DateTime? CampConfirmedAtUtc,
    string? CampConfirmedByName,
    string? PhysicalBadgeId,
    string Resume,
    DateTime? UpdatedAtUtc,
    string? UpdatedByName);

/// <summary>A вмілість the person holds, confirmed.</summary>
/// <param name="DuringProbe">Confirmed on or after the day the third probe opened.</param>
public sealed record ThirdProbeBadgeResponse(
    string BadgeId,
    string Title,
    string? ImagePath,
    string? Specialization,
    DateTime? ConfirmedAtUtc,
    bool DuringProbe);

/// <summary>The limits as numbers, so a client says the same thing the server enforces.</summary>
public sealed record ProbeProjectLimitsResponse(
    int MaxMainProjects,
    int MaxSpareProjects,
    int MaxSharedMainProjects,
    int MaxImagesPerProject,
    long MaxImageBytes,
    long MaxCertificateBytes,
    int MaxTextLength,
    int RequiredBadges,
    int YouthEndsAtAge);

/// <summary>Everything the «Третя проба» tab shows.</summary>
/// <param name="IsProjectProbe">Whether the probe stands at the third place of the person's line.
/// Only that one has a file and projects; for any other the rest is empty.</param>
/// <param name="CanEdit">The person themself, or whoever the kurin lets sign their probe.</param>
/// <param name="CanSign">May confirm the conference, the camp, a project's start and its completion.</param>
/// <param name="CanUnsign">May take such a confirmation back.</param>
public sealed record ThirdProbeResponse(
    bool IsProjectProbe,
    bool CanEdit,
    bool CanSign,
    bool CanUnsign,
    ThirdProbeFileResponse? File,
    IReadOnlyList<ProbeProjectResponse> Projects,
    IReadOnlyList<ThirdProbeBadgeResponse> Badges,
    ProbeProjectLimitsResponse Limits);
