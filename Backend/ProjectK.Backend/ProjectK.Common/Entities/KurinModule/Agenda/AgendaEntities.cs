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

    public Guid CreatedByUserKey { get; set; }

    public Kurin Kurin { get; set; } = null!;
    public ICollection<AgendaAssignment> Assignments { get; set; } = new List<AgendaAssignment>();
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
