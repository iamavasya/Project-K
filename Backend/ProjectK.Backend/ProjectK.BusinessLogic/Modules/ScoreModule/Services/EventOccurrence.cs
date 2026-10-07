using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.ScoreModule.Services;

/// <summary>One occurrence of an event of the kurin, checked to be a day the series really falls on.</summary>
public sealed record EventOccurrence(AgendaItem Item, DateTime StartUtc, DateTime? EndUtc)
{
    /// <summary>
    /// Null when there is no such event here, it is a task, or the series has no occurrence starting
    /// at <paramref name="occurrenceStartUtc"/>: a mark on a day nobody met would be a mark on nothing.
    /// </summary>
    public static async Task<EventOccurrence?> LoadAsync(
        IUnitOfWork unitOfWork,
        Guid kurinKey,
        Guid agendaItemKey,
        DateTime occurrenceStartUtc,
        CancellationToken cancellationToken)
    {
        var item = await unitOfWork.AgendaItems.GetByKeyWithAssignmentsAsync(agendaItemKey, cancellationToken);
        if (item is null || item.KurinKey != kurinKey || item.Kind != AgendaItemKind.Event || !item.StartUtc.HasValue)
        {
            return null;
        }

        var wanted = DateTime.SpecifyKind(occurrenceStartUtc, DateTimeKind.Utc);
        var occurrence = AgendaRecurrence.Expand(item, wanted.AddDays(-1), wanted.AddDays(1))
            .FirstOrDefault(o => DateTime.SpecifyKind(o.StartUtc, DateTimeKind.Utc) == wanted);

        if (occurrence == default)
        {
            return null;
        }

        DateTime? endUtc = occurrence.EndUtc is { } end ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : null;
        return new EventOccurrence(item, wanted, endUtc);
    }
}
