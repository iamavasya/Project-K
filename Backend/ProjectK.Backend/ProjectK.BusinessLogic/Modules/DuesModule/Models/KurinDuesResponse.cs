using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Models;

/// <summary>
/// The kurin's side of the вкладка in one read: its own box, what each гурток owes up and has handed
/// over, the transfers waiting to be confirmed, and its own operations.
/// </summary>
public sealed class KurinDuesResponse
{
    public Guid KurinKey { get; init; }
    public QuarterDto CurrentQuarter { get; init; } = new();
    public IReadOnlyList<PlastYearDto> Years { get; init; } = [];
    public IReadOnlyList<KurinDuesRateDto> Rates { get; init; } = [];

    /// <summary><c>ToForward</c> here is what still has to go to the станиця; <c>InTransit</c> — handed over by гуртки, not yet confirmed.</summary>
    public DuesBoxDto Box { get; init; } = new();

    public decimal SentToStanytsia { get; init; }

    public IReadOnlyList<KurinGroupHandoverDto> Groups { get; init; } = [];
    public IReadOnlyList<DuesTransferDto> Transfers { get; init; } = [];

    /// <summary>The kurin's own operations — not the гуртки's.</summary>
    public IReadOnlyList<DuesEntryDto> Entries { get; init; } = [];

    public IReadOnlyList<DuesPersonDto> People { get; init; } = [];
    public KurinDuesViewerDto Viewer { get; init; } = new();
}

/// <summary>How one гурток stands with the kurin.</summary>
public sealed class KurinGroupHandoverDto
{
    public Guid GroupKey { get; init; }
    public string GroupName { get; init; } = string.Empty;

    /// <summary>Collected by the гурток for the станиця and the kurin so far.</summary>
    public decimal OwedUp { get; init; }
    public decimal Transferred { get; init; }
    public decimal Received { get; init; }

    /// <summary>Collected but not handed over yet — still in the гурток's box.</summary>
    public decimal Outstanding { get; init; }

    /// <summary>Handed over, not yet confirmed by the kurin.</summary>
    public decimal InTransit { get; init; }
}

/// <summary>A гурток's transfer to the kurin, as the kurin sees it.</summary>
public sealed class DuesTransferDto
{
    public Guid DuesEntryKey { get; init; }
    public Guid GroupKey { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DuesPaymentMethod Method { get; init; }
    public DateOnly OccurredOn { get; init; }
    public string? CollectedByName { get; init; }
    public string? Note { get; init; }
    public bool IsReceived { get; init; }
    public DateTime? ReceivedAtUtc { get; init; }
    public string? ReceivedByName { get; init; }
}

public sealed class KurinDuesViewerDto
{
    /// <summary>May write the kurin's own operations and confirm transfers — the курінний скарбник and the Звʼязковий.</summary>
    public bool CanKeep { get; init; }

    /// <summary>May mark a kurin operation verified — the Звʼязковий.</summary>
    public bool CanVerify { get; init; }

    public bool CanSetRates { get; init; }
}
