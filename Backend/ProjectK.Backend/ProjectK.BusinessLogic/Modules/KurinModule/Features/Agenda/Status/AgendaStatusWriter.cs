using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Events;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Status;

/// <summary>
/// Writes one move — a whole target or one person's part — and re-derives what it adds up to, so the
/// target's and the task's stored states never lag behind the part that moved.
/// </summary>
internal static class AgendaStatusWriter
{
    public static void Apply(
        IUnitOfWork uow,
        AgendaItem item,
        AgendaStake stake,
        AgendaItemStatus status,
        Guid actorUserKey,
        AgendaRoster roster,
        DateTime nowUtc)
    {
        var assignment = stake.Assignment;
        if (stake.MemberKey is { } memberKey)
        {
            var part = assignment.Progress.FirstOrDefault(p => p.MemberKey == memberKey);
            if (part is null)
            {
                part = new AgendaAssignmentProgress
                {
                    AgendaAssignmentKey = assignment.AgendaAssignmentKey,
                    MemberKey = memberKey
                };
                uow.AgendaItems.AddProgress(part);
                // A tracked assignment picks the new row up by fix-up on Add; adding it again would count it twice.
                if (!assignment.Progress.Contains(part))
                {
                    assignment.Progress.Add(part);
                }
            }

            part.Status = status;
            part.ChangedByUserKey = actorUserKey;
            part.ChangedAtUtc = nowUtc;
            assignment.Status = AgendaCompletion.TargetStatus(assignment, roster);
        }
        else
        {
            assignment.Status = status;
        }

        assignment.StatusChangedByUserKey = actorUserKey;
        assignment.StatusChangedAtUtc = nowUtc;
        AgendaCompletion.SetItemStatus(item, AgendaCompletion.ItemStatus(item, roster), nowUtc);
        item.UpdatedDate = nowUtc;
    }

    /// <summary>The author wants to know when the task they set moves as a whole — not when they moved it themselves.</summary>
    public static async Task PublishAsync(IDomainEventPublisher events, AgendaItem item, Guid actorUserKey, CancellationToken cancellationToken)
    {
        if (item.CreatedByUserKey == Guid.Empty || item.CreatedByUserKey == actorUserKey)
        {
            return;
        }

        await events.PublishAsync(
            new AgendaItemStatusChanged(
                item.AgendaItemKey,
                item.KurinKey,
                item.Kind,
                item.Title,
                item.Status,
                item.CreatedByUserKey,
                actorUserKey),
            cancellationToken);
    }
}
