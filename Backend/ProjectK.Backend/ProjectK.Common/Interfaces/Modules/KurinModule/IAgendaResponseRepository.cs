using ProjectK.Common.Entities.KurinModule.Agenda;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

public interface IAgendaResponseRepository : IBaseEntityRepository<AgendaResponse>
{
    /// <summary>Every RSVP on an event, oldest first, so confirmed-vs-waitlist can be ranked by time.</summary>
    Task<IReadOnlyList<AgendaResponse>> GetForItemAsync(Guid agendaItemKey, CancellationToken cancellationToken = default);

    /// <summary>The current user's RSVP on an item, if any.</summary>
    Task<AgendaResponse?> GetForItemAndUserAsync(Guid agendaItemKey, Guid userKey, CancellationToken cancellationToken = default);
}
