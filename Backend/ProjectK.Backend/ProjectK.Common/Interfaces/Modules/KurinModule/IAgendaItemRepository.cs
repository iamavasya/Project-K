using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

public interface IAgendaItemRepository : IBaseEntityRepository<AgendaItem>
{
    /// <summary>Loads an item with its assignments for detail, edit and authorization checks.</summary>
    Task<AgendaItem?> GetByKeyWithAssignmentsAsync(Guid agendaItemKey, CancellationToken token = default);

    /// <summary>Marks an assignment for insertion. Explicit Added state avoids collection-fixup ambiguity.</summary>
    void AddAssignment(AgendaAssignment assignment);

    /// <summary>Marks an assignment for deletion. Explicit Deleted state avoids an accidental UPDATE.</summary>
    void RemoveAssignment(AgendaAssignment assignment);

    /// <summary>
    /// Items visible to <paramref name="viewer"/>. <paramref name="onlyDated"/> keeps the calendar to
    /// placed items; <paramref name="fromUtc"/>/<paramref name="toUtc"/> narrow to a window;
    /// <paramref name="kind"/> narrows the board to tasks. Assignments are included.
    /// </summary>
    Task<IEnumerable<AgendaItem>> GetForViewerAsync(
        AgendaViewerScope viewer,
        DateTime? fromUtc,
        DateTime? toUtc,
        bool onlyDated,
        AgendaItemKind? kind,
        CancellationToken token = default);
}
