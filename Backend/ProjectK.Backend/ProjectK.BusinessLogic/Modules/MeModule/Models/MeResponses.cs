using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.MeModule.Models;

/// <summary>The kurin a row belongs to — shown only when the person stands in more than one.</summary>
public sealed class MyKurinRefDto
{
    public Guid KurinKey { get; set; }
    public int KurinNumber { get; set; }
    public string? NamedAfter { get; set; }
    /// <summary>Whether this is the kurin the token acts in; what can be done from the dashboard depends on it.</summary>
    public bool IsCurrent { get; set; }
}

/// <summary>One occurrence of an event ahead, from any of the person's kurins.</summary>
public sealed class MyEventDto
{
    public Guid AgendaItemKey { get; set; }
    public MyKurinRefDto Kurin { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsRecurring { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryColorHex { get; set; }
    public string? CategoryIcon { get; set; }
    /// <summary>Whether the group of events asks for an answer.</summary>
    public bool RsvpRequired { get; set; }
    public AgendaRsvpStatus? MyResponse { get; set; }
}

/// <summary>A task the person is on the hook for, or raised, that is not done.</summary>
public sealed class MyTaskDto
{
    public Guid AgendaItemKey { get; set; }
    public MyKurinRefDto Kurin { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public AgendaItemStatus Status { get; set; }
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    /// <summary>False when the person sees the task only as its author.</summary>
    public bool AddressedToMe { get; set; }
    /// <summary>Whether the status can be moved from here: the right to, in the kurin the token acts in.</summary>
    public bool CanChangeStatus { get; set; }
}

// --- Personal growth: проба and вмілості ---

/// <summary>
/// The youth programme as it stands for the person: the проба they are on and their вмілості. Only
/// for someone whose own branch is УПЮ; for anyone else <see cref="HasYouthProgram"/> is false and
/// the rest is empty, so the dashboard shows nothing of it.
/// </summary>
public sealed class MyGrowthDto
{
    public Guid MemberKey { get; set; }
    public bool HasYouthProgram { get; set; }
    /// <summary>The проба in hand, or the next one to start; null once every проба is verified.</summary>
    public MyProbeDto? Probe { get; set; }
    public MyBadgesDto Badges { get; set; } = new();
}

public sealed class MyProbeDto
{
    public string ProbeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ProbeProgressStatus Status { get; set; }
    public int SignedPoints { get; set; }
    public int TotalPoints { get; set; }
    /// <summary>The next few points still to be signed, in the order of the проба.</summary>
    public IReadOnlyList<MyProbePointDto> NextPoints { get; set; } = [];
}

public sealed class MyProbePointDto
{
    public string PointId { get; set; } = string.Empty;
    public string SectionCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public sealed class MyBadgesDto
{
    /// <summary>Handed in and waiting for the впорядник.</summary>
    public IReadOnlyList<MyBadgeDto> OnReview { get; set; } = [];
    /// <summary>Begun and not yet handed in, or sent back.</summary>
    public IReadOnlyList<MyBadgeDto> InWork { get; set; } = [];
    /// <summary>The latest confirmed, newest first.</summary>
    public IReadOnlyList<MyBadgeDto> Confirmed { get; set; } = [];
    public int ConfirmedCount { get; set; }
}

public sealed class MyBadgeDto
{
    public string BadgeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public BadgeProgressStatus Status { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}

// --- Вкладка ---

/// <summary>The person's вкладка in one kurin: what they owe or have over, and what this quarter costs.</summary>
public sealed class MyDuesDto
{
    public MyKurinRefDto Kurin { get; set; } = new();
    public string? GroupName { get; set; }
    public int QuarterYear { get; set; }
    public int QuarterNumber { get; set; }
    /// <summary>Across every гурток of the kurin: negative is debt, positive a surplus.</summary>
    public decimal Balance { get; set; }
    /// <summary>What the running quarter costs them; null when they stand in no гурток.</summary>
    public decimal? QuarterRate { get; set; }
    public bool IsConcession { get; set; }
}

// --- Точкування ---

/// <summary>The person's own points this пластовий рік in one kurin, and where their гурток stands.</summary>
public sealed class MyScoreDto
{
    public MyKurinRefDto Kurin { get; set; } = new();
    public Guid GroupKey { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public int Total { get; set; }
    public IReadOnlyDictionary<ScoreSource, int> BySource { get; set; } = new Dictionary<ScoreSource, int>();
    public int GroupPlace { get; set; }
    public int GroupCount { get; set; }
    public decimal GroupScore { get; set; }
    public ScoreAlgorithm Algorithm { get; set; }
}
