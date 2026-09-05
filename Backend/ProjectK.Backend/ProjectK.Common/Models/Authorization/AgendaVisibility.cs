using System.Linq.Expressions;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Models.Authorization;

/// <summary>
/// The single definition of which agenda items a viewer may see.
/// <para>
/// The rule used to exist twice — as an EF filter in the repository's feed query and again as an
/// in-memory predicate guarding RSVP — so the list a user saw and the items they were allowed to
/// answer could drift apart. It lives in Common because both the Infrastructure query and the
/// BusinessLogic guard have to agree on it.
/// </para>
/// </summary>
public static class AgendaVisibility
{
    /// <summary>
    /// Items carrying at least one assignment that reaches the viewer. Expressed as an expression tree
    /// so EF Core can translate it into the feed query; callers holding an already-loaded item should
    /// use <see cref="IsVisible"/> instead.
    /// <para>
    /// Does not cover whole-kurin viewers — they see everything, which both call sites short-circuit
    /// before narrowing.
    /// </para>
    /// </summary>
    public static Expression<Func<AgendaItem, bool>> AssignedToViewer(AgendaViewerScope viewer)
    {
        var groupKeys = viewer.ViewerGroupKeys;
        var leadershipKeys = viewer.ViewerLeadershipKeys;
        var memberKey = viewer.ViewerMemberKey;

        return item => item.Assignments.Any(assignment =>
            assignment.TargetType == AgendaTargetType.Kurin ||
            (assignment.TargetType == AgendaTargetType.Group && groupKeys.Contains(assignment.TargetKey)) ||
            (assignment.TargetType == AgendaTargetType.Leadership && leadershipKeys.Contains(assignment.TargetKey)) ||
            (assignment.TargetType == AgendaTargetType.Member && memberKey != null && assignment.TargetKey == memberKey));
    }

    /// <summary>
    /// Evaluates the same rule against an item whose <see cref="AgendaItem.Assignments"/> are loaded.
    /// <para>
    /// Compiling the expression costs a fraction of a millisecond and happens once per guarded request,
    /// against a handler that is already waiting on database I/O — cheap next to letting the rule be
    /// written a second time by hand.
    /// </para>
    /// </summary>
    public static bool IsVisible(AgendaItem item, AgendaViewerScope viewer)
        => viewer.CanSeeWholeKurin || AssignedToViewer(viewer).Compile()(item);
}
