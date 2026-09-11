using MediatR;
using ProjectK.BusinessLogic.Services.Events;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Events;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;

/// <summary>
/// Where an agenda change turns into an inbox entry. The route and the wording are worked out here
/// from what the event says — a task belongs to the board, an event to the calendar — so the agenda
/// itself no longer holds a screen path.
/// </summary>
internal static class AgendaNotificationText
{
    public static string RouteFor(AgendaItemKind kind, Guid kurinKey) =>
        kind == AgendaItemKind.Task ? $"/tasks/{kurinKey}" : $"/calendar/{kurinKey}";

    public static string KindWord(AgendaItemKind kind) =>
        kind == AgendaItemKind.Task ? "Задачу" : "Подію";
}

public sealed class AgendaItemAssignedEventHandler
    : INotificationHandler<DomainEventNotification<AgendaItemAssigned>>
{
    private readonly INotificationService _notifications;

    public AgendaItemAssignedEventHandler(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(DomainEventNotification<AgendaItemAssigned> notification, CancellationToken cancellationToken)
    {
        var raised = notification.Event;
        if (raised.RecipientUserKeys.Count == 0)
        {
            return;
        }

        await _notifications.NotifyManyAsync(
            raised.RecipientUserKeys.Select(userKey => new NotificationRequest
            {
                RecipientUserKey = userKey,
                Type = AppNotificationType.AgendaItemAssigned,
                Severity = AppNotificationSeverity.Info,
                Title = $"{AgendaNotificationText.KindWord(raised.Kind)} призначено вам",
                Body = raised.Title,
                EntityType = "AgendaItem",
                EntityKey = raised.AgendaItemKey,
                Route = AgendaNotificationText.RouteFor(raised.Kind, raised.KurinKey),
                ActorUserKey = raised.ActorUserKey,
                DeduplicationKey = $"agenda-assigned:{raised.AgendaItemKey}:{userKey}"
            }),
            cancellationToken);
    }
}

public sealed class AgendaItemChangedEventHandler
    : INotificationHandler<DomainEventNotification<AgendaItemChanged>>
{
    private readonly INotificationService _notifications;

    public AgendaItemChangedEventHandler(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(DomainEventNotification<AgendaItemChanged> notification, CancellationToken cancellationToken)
    {
        var raised = notification.Event;
        if (raised.RecipientUserKeys.Count == 0)
        {
            return;
        }

        await _notifications.NotifyManyAsync(
            raised.RecipientUserKeys.Select(userKey => new NotificationRequest
            {
                RecipientUserKey = userKey,
                Type = AppNotificationType.AgendaItemUpdated,
                Severity = AppNotificationSeverity.Info,
                Title = "Оновлено призначення",
                Body = raised.Title,
                EntityType = "AgendaItem",
                EntityKey = raised.AgendaItemKey,
                Route = AgendaNotificationText.RouteFor(raised.Kind, raised.KurinKey),
                ActorUserKey = raised.ActorUserKey,
                DeduplicationKey = $"agenda-updated:{raised.AgendaItemKey}:{userKey}"
            }),
            cancellationToken);
    }
}

public sealed class AgendaItemRemovedEventHandler
    : INotificationHandler<DomainEventNotification<AgendaItemRemoved>>
{
    private readonly INotificationService _notifications;

    public AgendaItemRemovedEventHandler(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(DomainEventNotification<AgendaItemRemoved> notification, CancellationToken cancellationToken)
    {
        var raised = notification.Event;
        if (raised.RecipientUserKeys.Count == 0)
        {
            return;
        }

        await _notifications.NotifyManyAsync(
            raised.RecipientUserKeys.Select(userKey => new NotificationRequest
            {
                RecipientUserKey = userKey,
                Type = AppNotificationType.AgendaItemDeleted,
                Severity = AppNotificationSeverity.Warn,
                Title = $"{AgendaNotificationText.KindWord(raised.Kind)} видалено",
                Body = raised.Title,
                EntityType = "AgendaItem",
                EntityKey = raised.AgendaItemKey,
                Route = AgendaNotificationText.RouteFor(raised.Kind, raised.KurinKey),
                ActorUserKey = raised.ActorUserKey,
                DeduplicationKey = $"agenda-deleted:{raised.AgendaItemKey}:{userKey}"
            }),
            cancellationToken);
    }
}

public sealed class AgendaItemStatusChangedEventHandler
    : INotificationHandler<DomainEventNotification<AgendaItemStatusChanged>>
{
    private readonly INotificationService _notifications;

    public AgendaItemStatusChangedEventHandler(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public Task Handle(DomainEventNotification<AgendaItemStatusChanged> notification, CancellationToken cancellationToken)
    {
        var raised = notification.Event;

        return _notifications.NotifyAsync(
            new NotificationRequest
            {
                RecipientUserKey = raised.CreatorUserKey,
                Type = AppNotificationType.AgendaItemStatusChanged,
                Severity = raised.Status == AgendaItemStatus.Done
                    ? AppNotificationSeverity.Success
                    : AppNotificationSeverity.Info,
                Title = "Статус задачі змінено",
                Body = raised.Title,
                EntityType = "AgendaItem",
                EntityKey = raised.AgendaItemKey,
                Route = AgendaNotificationText.RouteFor(raised.Kind, raised.KurinKey),
                ActorUserKey = raised.ActorUserKey,
                DeduplicationKey = $"agenda-status:{raised.AgendaItemKey}"
            },
            cancellationToken);
    }
}
