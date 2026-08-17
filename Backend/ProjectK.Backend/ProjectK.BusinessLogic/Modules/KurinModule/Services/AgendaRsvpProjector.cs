using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>
/// Turns raw RSVP rows into the display picture. Confirmed-vs-waitlist is derived here, not stored: the
/// «Going» answers are ranked by time and the first <c>capacity</c> are confirmed, the rest waitlisted
/// (only when the category has a capacity and the waitlist is enabled). NotGoing/Maybe are never waitlisted.
/// </summary>
public static class AgendaRsvpProjector
{
    public static AgendaResponsesResponse Project(
        Guid agendaItemKey,
        IReadOnlyList<AgendaResponse> responses,
        int? capacity,
        bool waitlistEnabled,
        IReadOnlyDictionary<Guid, string> nameByUserKey,
        Guid? myUserKey)
    {
        var going = responses
            .Where(r => r.Status == AgendaRsvpStatus.Going)
            .OrderBy(r => r.RespondedAtUtc)
            .ToList();

        var confirmedCount = capacity.HasValue ? Math.Min(going.Count, capacity.Value) : going.Count;
        var waitlistCount = going.Count - confirmedCount;

        var dtos = new List<AgendaRsvpDto>(responses.Count);
        for (var i = 0; i < going.Count; i++)
        {
            var r = going[i];
            dtos.Add(ToDto(r, nameByUserKey, isWaitlisted: capacity.HasValue && waitlistEnabled && i >= capacity.Value));
        }

        foreach (var r in responses.Where(r => r.Status != AgendaRsvpStatus.Going).OrderBy(r => r.RespondedAtUtc))
        {
            dtos.Add(ToDto(r, nameByUserKey, isWaitlisted: false));
        }

        return new AgendaResponsesResponse
        {
            AgendaItemKey = agendaItemKey,
            Capacity = capacity,
            WaitlistEnabled = waitlistEnabled,
            MyStatus = myUserKey.HasValue
                ? responses.FirstOrDefault(r => r.UserKey == myUserKey.Value)?.Status
                : null,
            GoingConfirmedCount = confirmedCount,
            GoingWaitlistCount = waitlistCount,
            NotGoingCount = responses.Count(r => r.Status == AgendaRsvpStatus.NotGoing),
            MaybeCount = responses.Count(r => r.Status == AgendaRsvpStatus.Maybe),
            Responses = dtos
        };
    }

    private static AgendaRsvpDto ToDto(AgendaResponse r, IReadOnlyDictionary<Guid, string> names, bool isWaitlisted) => new()
    {
        UserKey = r.UserKey,
        DisplayName = names.TryGetValue(r.UserKey, out var name) ? name : "—",
        Status = r.Status,
        RespondedAtUtc = r.RespondedAtUtc,
        IsWaitlisted = isWaitlisted
    };
}
