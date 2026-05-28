using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Services.Reports;

public sealed record KurinReportData(
    KurinReportHeader Header,
    KurinReportKurin Kurin,
    IReadOnlyList<KurinReportGroup> Groups,
    IReadOnlyList<KurinReportMember> KeyVolunteers,
    IReadOnlyList<KurinReportMember> Members);

public sealed record KurinReportHeader(
    DateTime GeneratedAtUtc,
    string GeneratedByName,
    string? GeneratedByEmail,
    string BackendVersion,
    string BackendCodename);

public sealed record KurinReportKurin(
    Guid KurinKey,
    int Number,
    string? Stanytsia,
    string? RegionOrCountry,
    string? NamedAfter,
    string? Description,
    bool IsZbtKurin,
    int ZbtUserCap);

public sealed record KurinReportGroup(
    Guid GroupKey,
    string Name,
    string? Description,
    string? SilhouetteUrl,
    byte[]? SilhouetteBytes,
    IReadOnlyList<string> MentorNames,
    IReadOnlyList<KurinReportGroupMember> Members);

public sealed record KurinReportGroupMember(
    Guid MemberKey,
    string FullName,
    string Email,
    string? PhoneNumber,
    PlastLevel? LatestPlastLevel);

public sealed record KurinReportMember(
    Guid MemberKey,
    Guid? UserKey,
    Guid? GroupKey,
    string? GroupName,
    string FullName,
    string Initials,
    string Email,
    string PhoneNumber,
    DateOnly DateOfBirth,
    string? Address,
    string? School,
    string? ProfilePhotoUrl,
    byte[]? ProfilePhotoBytes,
    PlastLevel? LatestPlastLevel,
    IReadOnlyList<string> SystemRoles,
    IReadOnlyList<KurinReportPlastLevel> PlastLevels,
    IReadOnlyList<KurinReportProbe> Probes,
    IReadOnlyList<KurinReportProbePoint> SignedProbePoints,
    IReadOnlyList<KurinReportBadge> ConfirmedBadges,
    IReadOnlyList<KurinReportWarning> ActiveWarnings,
    IReadOnlyList<KurinReportAward> Awards,
    IReadOnlyList<KurinReportLeadershipHistory> LeadershipHistory);

public sealed record KurinReportPlastLevel(PlastLevel PlastLevel, DateOnly DateAchieved);

public sealed record KurinReportProbe(
    string ProbeId,
    string ProbeTitle,
    ProbeProgressStatus Status,
    string StatusLabel,
    DateTime? CompletedAtUtc,
    string? CompletedByName,
    DateTime? VerifiedAtUtc,
    string? VerifiedByName);

public sealed record KurinReportProbePoint(
    string ProbeId,
    string ProbeTitle,
    string PointId,
    string PointLabel,
    DateTime? SignedAtUtc,
    string? SignedByName,
    string? SignedByRole);

public sealed record KurinReportBadge(
    string BadgeId,
    string BadgeTitle,
    BadgeProgressStatus Status,
    string StatusLabel,
    DateTime? ReviewedAtUtc,
    string? ReviewedByName,
    string? ReviewedByRole);

public sealed record KurinReportWarning(
    MemberWarningLevel Level,
    string LevelLabel,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    bool IsRevoked);

public sealed record KurinReportAward(
    MemberAwardLevel Level,
    string LevelLabel,
    DateTime DateAcquired,
    string? Note,
    BadgeProgressStatus Status,
    string StatusLabel);

public sealed record KurinReportLeadershipHistory(
    LeadershipType Type,
    string TypeLabel,
    LeadershipRole Role,
    string RoleLabel,
    string? ScopeName,
    DateOnly StartDate,
    DateOnly? EndDate);
