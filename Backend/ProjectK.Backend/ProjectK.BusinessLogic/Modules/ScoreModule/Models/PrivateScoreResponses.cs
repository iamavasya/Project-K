namespace ProjectK.BusinessLogic.Modules.ScoreModule.Models;

public sealed class PrivateScoreCriterionDto
{
    public Guid PrivateScoreCriterionKey { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
}

public sealed class PrivateScoreEntryDto
{
    public Guid PrivateScoreEntryKey { get; set; }
    public Guid MembershipKey { get; set; }
    public Guid MemberKey { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid? PrivateScoreCriterionKey { get; set; }
    public string? CriterionName { get; set; }
    public int Points { get; set; }
    public string? Note { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>One youth as the КВ sees them: the public points as the ground, the private ones on top.</summary>
public sealed class PrivateScorePersonDto
{
    public Guid MembershipKey { get; set; }
    public Guid MemberKey { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Guid? GroupKey { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int PublicTotal { get; set; }
    public int PrivateTotal { get; set; }
    public IReadOnlyDictionary<Guid, int> ByCriterion { get; set; } = new Dictionary<Guid, int>();
    /// <summary>What was written with no criterion.</summary>
    public int Uncategorised { get; set; }
}

public sealed class PrivateScoreResponse
{
    public Guid KurinKey { get; set; }
    public ScorePeriodDto Period { get; set; } = new();
    public ScorePeriodsDto Periods { get; set; } = new();
    public IReadOnlyList<PrivateScoreCriterionDto> Criteria { get; set; } = [];
    public IReadOnlyList<PrivateScorePersonDto> People { get; set; } = [];
    public IReadOnlyList<PrivateScoreEntryDto> Entries { get; set; } = [];
}
