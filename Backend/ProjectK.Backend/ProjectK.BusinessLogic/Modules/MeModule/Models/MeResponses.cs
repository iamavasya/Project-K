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
