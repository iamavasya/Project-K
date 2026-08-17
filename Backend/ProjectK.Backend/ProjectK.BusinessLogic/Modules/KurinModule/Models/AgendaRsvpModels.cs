using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models;

/// <summary>One person's answer, with display name and — for «Going» — whether capacity pushed them to the waitlist.</summary>
public record AgendaRsvpDto
{
    public Guid UserKey { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public AgendaRsvpStatus Status { get; init; }
    public DateTime RespondedAtUtc { get; init; }

    /// <summary>True for a «Going» answer beyond the category capacity; always false for NotGoing/Maybe.</summary>
    public bool IsWaitlisted { get; init; }
}

/// <summary>The full RSVP picture for one event: everyone's answer, the counts, and the caller's own choice.</summary>
public record AgendaResponsesResponse
{
    public Guid AgendaItemKey { get; init; }
    public int? Capacity { get; init; }
    public bool WaitlistEnabled { get; init; }
    public AgendaRsvpStatus? MyStatus { get; init; }
    public int GoingConfirmedCount { get; init; }
    public int GoingWaitlistCount { get; init; }
    public int NotGoingCount { get; init; }
    public int MaybeCount { get; init; }
    public List<AgendaRsvpDto> Responses { get; init; } = [];
}
