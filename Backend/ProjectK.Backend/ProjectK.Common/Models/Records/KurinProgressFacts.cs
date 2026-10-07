using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Records;

/// <summary>A вмілість confirmed to someone of the kurin, and when.</summary>
public sealed record BadgeConfirmedRecord(Guid MemberKey, string BadgeId, DateTime ConfirmedAtUtc);

/// <summary>A point of a проба signed off for someone of the kurin, and when.</summary>
public sealed record ProbePointSignedRecord(Guid MemberKey, string ProbeId, string PointId, DateTime SignedAtUtc);

/// <summary>A whole проба verified for someone of the kurin, and when.</summary>
public sealed record ProbeClosedRecord(Guid MemberKey, string ProbeId, DateTime ClosedAtUtc);

/// <summary>
/// What the probes and вмілості module knows was earned in one kurin — the facts точкування
/// prices. Read whole, kurin by kurin, so the score needs one call and not one per person.
/// </summary>
public sealed record KurinProgressFacts(
    IReadOnlyCollection<BadgeConfirmedRecord> Badges,
    IReadOnlyCollection<ProbePointSignedRecord> ProbePoints,
    IReadOnlyCollection<ProbeClosedRecord> Probes)
{
    public static KurinProgressFacts Empty { get; } = new([], [], []);
}

/// <summary>A quarter of вкладка that one membership closed with nothing owed, and the day it closed.</summary>
public sealed record PaidQuarterRecord(Guid MembershipKey, int QuarterIndex, DateOnly LastDay);

/// <summary>A пересторога in force: whose, what level, since when.</summary>
public sealed record WarningRecord(Guid MemberKey, MemberWarningLevel Level, DateTime IssuedAtUtc);
