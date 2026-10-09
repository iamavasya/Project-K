using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Records;

/// <summary>One проба with the names the звіт prints: who closed it and who verified it.</summary>
public sealed record ProbeProgressDetail(
    string ProbeId,
    ProbeProgressStatus Status,
    DateTime? CompletedAtUtc,
    string? CompletedByName,
    DateTime? VerifiedAtUtc,
    string? VerifiedByName);

/// <summary>One signed point of a проба, with who signed it and in what capacity.</summary>
public sealed record ProbePointSignedDetail(
    string ProbeId,
    string PointId,
    DateTime? SignedAtUtc,
    string? SignedByName,
    string? SignedByRole);

/// <summary>One confirmed вмілість, with who confirmed it and in what capacity.</summary>
public sealed record BadgeConfirmedDetail(
    string BadgeId,
    BadgeProgressStatus Status,
    DateTime? ReviewedAtUtc,
    string? ReviewedByName,
    string? ReviewedByRole);

/// <summary>
/// Everything the звіт куреня prints about one person's progress: the проби as far as they went,
/// the points signed, the вмілості confirmed. Only what is printed — a draft or a rejected вмілість
/// is not in it.
/// </summary>
public sealed record MemberProgressDetail(
    IReadOnlyList<ProbeProgressDetail> Probes,
    IReadOnlyList<ProbePointSignedDetail> SignedPoints,
    IReadOnlyList<BadgeConfirmedDetail> ConfirmedBadges)
{
    public static MemberProgressDetail Empty { get; } = new([], [], []);
}
