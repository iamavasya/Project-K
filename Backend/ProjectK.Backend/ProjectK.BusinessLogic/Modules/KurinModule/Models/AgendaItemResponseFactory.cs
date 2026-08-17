using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Models.Enums;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Models;

/// <summary>
/// Builds <see cref="AgendaItemResponse"/> from an entity plus name lookups and the viewer context.
/// Kept in one place so the calendar and board return identical shapes and per-viewer flags.
/// </summary>
public static class AgendaItemResponseFactory
{
    public static AgendaItemResponse Create(
        AgendaItem item,
        AgendaViewerContext viewer,
        string kurinLabel,
        IReadOnlyDictionary<Guid, string> groupNames,
        IReadOnlyDictionary<Guid, string> memberNames,
        IReadOnlyDictionary<Guid, string> creatorNames,
        IReadOnlyDictionary<Guid, string> leadershipLabels,
        IReadOnlyDictionary<Guid, AgendaCategory> categories,
        DateTime? occurrenceStartUtc = null,
        DateTime? occurrenceEndUtc = null)
    {
        AgendaCategory? category = null;
        if (item.AgendaCategoryKey.HasValue)
        {
            categories.TryGetValue(item.AgendaCategoryKey.Value, out category);
        }

        // For a recurring series the calendar shows one row per occurrence: the dates come from the
        // expansion, but the key stays the series key so edit/delete act on the whole series (v1).
        var isInstance = occurrenceStartUtc.HasValue;

        return new AgendaItemResponse
        {
            AgendaItemKey = item.AgendaItemKey,
            KurinKey = item.KurinKey,
            Kind = item.Kind,
            Title = item.Title,
            Description = item.Description,
            Status = item.Status,
            StartUtc = occurrenceStartUtc ?? item.StartUtc,
            EndUtc = isInstance ? occurrenceEndUtc : item.EndUtc,
            IsAllDay = item.IsAllDay,
            CreatedByUserKey = item.CreatedByUserKey,
            CreatedByName = creatorNames.TryGetValue(item.CreatedByUserKey, out var creator) ? creator : null,
            CanEdit = AgendaPermissions.CanManage(item, viewer),
            CanChangeStatus = AgendaPermissions.CanChangeStatus(item, viewer),
            CategoryKey = category?.AgendaCategoryKey,
            CategoryName = category?.Name,
            CategoryColorHex = category?.ColorHex,
            CategoryIcon = category?.Icon,
            RecurrenceFrequency = item.RecurrenceFrequency,
            RecurrenceInterval = item.RecurrenceInterval,
            RecurrenceByWeekday = item.RecurrenceByWeekday,
            RecurrenceEndUtc = item.RecurrenceEndUtc,
            RecurrenceCount = item.RecurrenceCount,
            IsRecurrenceInstance = isInstance,
            Assignments = item.Assignments
                .Select(a => new AgendaAssignmentDto
                {
                    TargetType = a.TargetType,
                    TargetKey = a.TargetKey,
                    Label = ResolveLabel(a, kurinLabel, groupNames, memberNames, leadershipLabels)
                })
                .ToList()
        };
    }

    private static string ResolveLabel(
        AgendaAssignment assignment,
        string kurinLabel,
        IReadOnlyDictionary<Guid, string> groupNames,
        IReadOnlyDictionary<Guid, string> memberNames,
        IReadOnlyDictionary<Guid, string> leadershipLabels)
    {
        return assignment.TargetType switch
        {
            AgendaTargetType.Kurin => kurinLabel,
            AgendaTargetType.Group => groupNames.TryGetValue(assignment.TargetKey, out var name) ? name : "—",
            AgendaTargetType.Member => memberNames.TryGetValue(assignment.TargetKey, out var name) ? name : "—",
            AgendaTargetType.Leadership => leadershipLabels.TryGetValue(assignment.TargetKey, out var name) ? name : "—",
            _ => "—"
        };
    }
}
