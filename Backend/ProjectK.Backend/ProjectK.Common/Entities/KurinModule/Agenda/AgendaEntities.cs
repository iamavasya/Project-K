using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.Entities;

namespace ProjectK.Common.Entities.KurinModule.Agenda;

/// <summary>
/// A calendar event or a board task inside a kurin. Dates are stored as full UTC even though the UI
/// renders them per-day, so switching to an hourly view later needs no migration. Who sees an item
/// is decided by its <see cref="Assignments"/>, not by a single owner column.
/// </summary>
public class AgendaItem : Entity
{
    public Guid AgendaItemKey { get; set; } = Guid.NewGuid();
    public Guid KurinKey { get; set; }
    public AgendaItemKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Board column. Meaningful for <see cref="AgendaItemKind.Task"/>; events stay Todo.</summary>
    public AgendaItemStatus Status { get; set; } = AgendaItemStatus.Todo;

    /// <summary>Full UTC start; null means the item is not yet placed on the calendar.</summary>
    public DateTime? StartUtc { get; set; }

    /// <summary>Full UTC end; null for a single-day item.</summary>
    public DateTime? EndUtc { get; set; }

    /// <summary>True while the calendar runs in per-day mode; kept so an hourly mode can honour it.</summary>
    public bool IsAllDay { get; set; } = true;

    /// <summary>The event group (табір/захід/сходини) this item belongs to; null when uncategorised.</summary>
    public Guid? AgendaCategoryKey { get; set; }

    public Guid CreatedByUserKey { get; set; }

    public Kurin Kurin { get; set; } = null!;
    public AgendaCategory? Category { get; set; }
    public ICollection<AgendaAssignment> Assignments { get; set; } = new List<AgendaAssignment>();
    public ICollection<AgendaResponse> Responses { get; set; } = new List<AgendaResponse>();
}

/// <summary>
/// A per-kurin event group (табір, захід, сходини…), curated by the Зв'язковий. Carries the visual
/// identity (colour + icon) and the defaults an event in the group inherits: capacity with an optional
/// waitlist, a description template, a default duration, whether an RSVP is expected, and a reminder lead.
/// </summary>
public class AgendaCategory : Entity
{
    public Guid AgendaCategoryKey { get; set; } = Guid.NewGuid();
    public Guid KurinKey { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Brand-token colour (hex) used to tint the group's events on the calendar.</summary>
    public string ColorHex { get; set; } = string.Empty;

    /// <summary>Icon name (optimus/pi icon) shown on the group's events to tell табір from захід at a glance.</summary>
    public string? Icon { get; set; }

    /// <summary>Max confirmed attendees; null means unlimited. When set, extra «Going» RSVPs form a waitlist.</summary>
    public int? Capacity { get; set; }
    public bool WaitlistEnabled { get; set; }

    /// <summary>Pre-filled description an event in this group starts from.</summary>
    public string? DefaultDescription { get; set; }

    /// <summary>Whether events in this group ask attendees for an RSVP.</summary>
    public bool RsvpRequired { get; set; }

    /// <summary>Default event length in minutes, used to pre-fill the end when creating an event.</summary>
    public int? DefaultDurationMinutes { get; set; }

    /// <summary>Minutes before start to remind attendees; null disables reminders for the group.</summary>
    public int? ReminderLeadMinutes { get; set; }

    /// <summary>Archived groups stay for historical items but are hidden from the picker.</summary>
    public bool IsArchived { get; set; }

    public Kurin Kurin { get; set; } = null!;
}

/// <summary>
/// One member's RSVP to an event. Uniqueness is (item, user): a fresh answer overwrites the previous
/// one. Confirmed-vs-waitlist is derived at read time from the category capacity and <see cref="RespondedAtUtc"/>,
/// not stored, so a capacity change re-ranks everyone without a migration.
/// </summary>
public class AgendaResponse : Entity
{
    public Guid AgendaResponseKey { get; set; } = Guid.NewGuid();
    public Guid AgendaItemKey { get; set; }
    public Guid UserKey { get; set; }
    public AgendaRsvpStatus Status { get; set; }
    public DateTime RespondedAtUtc { get; set; } = DateTime.UtcNow;

    public AgendaItem AgendaItem { get; set; } = null!;
}

/// <summary>
/// One target an agenda item is assigned to. An item carries many of these so it can be aimed at the
/// kurin, several groups and individual members simultaneously.
/// </summary>
public class AgendaAssignment : Entity
{
    public Guid AgendaAssignmentKey { get; set; } = Guid.NewGuid();
    public Guid AgendaItemKey { get; set; }
    public AgendaTargetType TargetType { get; set; }

    /// <summary>KurinKey, GroupKey or MemberKey, per <see cref="TargetType"/>.</summary>
    public Guid TargetKey { get; set; }

    public AgendaItem AgendaItem { get; set; } = null!;
}
