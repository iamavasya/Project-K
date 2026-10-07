using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>
/// One answer per (event, account): a fresh answer overwrites the old. Shared by the calendar and
/// the dashboard so both write the same row the same way. Does not save.
/// </summary>
public static class AgendaRsvpWriter
{
    public static async Task UpsertAsync(IUnitOfWork unitOfWork, AgendaItem item, Guid userKey, AgendaRsvpStatus status, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.AgendaResponses.GetForItemAndUserAsync(item.AgendaItemKey, userKey, cancellationToken);
        if (existing is null)
        {
            unitOfWork.AgendaResponses.Create(new AgendaResponse
            {
                AgendaItemKey = item.AgendaItemKey,
                UserKey = userKey,
                Status = status,
                RespondedAtUtc = nowUtc
            }, cancellationToken);
        }
        else if (existing.Status != status)
        {
            // Re-time on change so switching to «Going» joins the back of the queue rather than keeping
            // an earlier «Maybe» slot.
            existing.Status = status;
            existing.RespondedAtUtc = nowUtc;
            existing.UpdatedDate = nowUtc;
            unitOfWork.AgendaResponses.Update(existing, cancellationToken);
        }
    }
}
