using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.ScoreModule.Requests;

/// <summary>Which days a read totals: a пластовий рік by its start year, or a stage. Neither means the current year.</summary>
public sealed class ScorePeriodQuery
{
    public int? Year { get; set; }
    public Guid? StageKey { get; set; }
}

public sealed class SetScoreAlgorithmRequest
{
    public ScoreAlgorithm Algorithm { get; set; }
}

/// <summary>What an automatic source is worth from a day on; zero turns it off from then.</summary>
public sealed class SetScoreRuleRequest
{
    public ScoreSource Source { get; set; }

    /// <summary>The level for a пересторога; 0 otherwise.</summary>
    public int Variant { get; set; }

    public DateOnly FromDate { get; set; }
    public int Points { get; set; }
}

/// <summary>
/// What being at an event is worth, for a group of events or for one event; exactly one key is set.
/// Null points take an event's own rate off, so it is worth its group's again.
/// </summary>
public sealed class SetScoreAttendanceRateRequest
{
    public Guid? AgendaCategoryKey { get; set; }
    public Guid? AgendaItemKey { get; set; }
    public int? Points { get; set; }
}

public sealed class UpsertScoreItemRequest
{
    public string Name { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class UpsertScoreStageRequest
{
    public string Name { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

/// <summary>Marks these people present at one occurrence; those already marked are left as they are.</summary>
public sealed class MarkAttendanceRequest
{
    public IReadOnlyList<Guid> MembershipKeys { get; set; } = [];
}

/// <summary>
/// Points by hand: to a person or to a гурток (one of the two), as a position from the list or as a
/// free amount with a reason, at an event (then with its occurrence) or not.
/// </summary>
public sealed class UpsertScoreEntryRequest
{
    public Guid? MembershipKey { get; set; }
    public Guid? GroupKey { get; set; }
    public Guid? ScoreItemKey { get; set; }
    public int? Points { get; set; }
    public string? Reason { get; set; }
    public Guid? AgendaItemKey { get; set; }
    public DateTime? OccurrenceStartUtc { get; set; }
    public DateOnly OccurredOn { get; set; }
}
