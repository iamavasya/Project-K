using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Status;

public sealed record ChangeAgendaItemStatus(Guid AgendaItemKey, AgendaItemStatus Status)
    : IRequest<ServiceResult<object>>;

public sealed class ChangeAgendaItemStatusHandler : IRequestHandler<ChangeAgendaItemStatus, ServiceResult<object>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;
    private readonly ICurrentUserContext _currentUser;
    private readonly INotificationService _notifications;

    public ChangeAgendaItemStatusHandler(
        IUnitOfWork uow,
        IAgendaAccess access,
        ICurrentUserContext currentUser,
        INotificationService notifications)
    {
        _uow = uow;
        _access = access;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<ServiceResult<object>> Handle(ChangeAgendaItemStatus request, CancellationToken cancellationToken)
    {
        var item = await _uow.AgendaItems.GetByKeyWithAssignmentsAsync(request.AgendaItemKey, cancellationToken);
        if (item is null)
        {
            return ServiceResult<object>.Failure(ResultType.NotFound, "AGENDA_NOT_FOUND", "Agenda item was not found.");
        }

        var viewer = await _access.BuildViewerAsync(item.KurinKey, cancellationToken);
        if (viewer.KurinKey != _currentUser.KurinKey)
        {
            return ServiceResult<object>.Failure(ResultType.Forbidden, "AGENDA_OTHER_KURIN", "Agenda item belongs to a different kurin.");
        }

        if (!AgendaPermissions.CanChangeStatus(item, viewer))
        {
            return ServiceResult<object>.Failure(ResultType.Forbidden, "AGENDA_STATUS_FORBIDDEN", "You may change status only for your own tasks or those you lead.");
        }

        if (item.Status == request.Status)
        {
            return new ServiceResult<object>(ResultType.Success);
        }

        item.Status = request.Status;
        item.UpdatedDate = DateTime.UtcNow;
        // The item is tracked (loaded with assignments), so the status change is persisted without an
        // explicit Update() — which would re-mark the client-keyed assignments and fight change tracking.
        await _uow.SaveChangesAsync(cancellationToken);

        await NotifyStatusChangedAsync(item, viewer.ViewerUserKey ?? Guid.Empty, cancellationToken);

        return new ServiceResult<object>(ResultType.Success);
    }

    private async Task NotifyStatusChangedAsync(AgendaItem item, Guid actorUserKey, CancellationToken cancellationToken)
    {
        // The creator wants to know when a task they set moves; skip if the creator is the actor.
        if (item.CreatedByUserKey == Guid.Empty || item.CreatedByUserKey == actorUserKey)
        {
            return;
        }

        var severity = item.Status == AgendaItemStatus.Done
            ? AppNotificationSeverity.Success
            : AppNotificationSeverity.Info;

        await _notifications.NotifyAsync(new NotificationRequest
        {
            RecipientUserKey = item.CreatedByUserKey,
            Type = AppNotificationType.AgendaItemStatusChanged,
            Severity = severity,
            Title = "Статус задачі змінено",
            Body = item.Title,
            EntityType = "AgendaItem",
            EntityKey = item.AgendaItemKey,
            Route = AgendaRoutes.For(item),
            ActorUserKey = actorUserKey,
            DeduplicationKey = $"agenda-status:{item.AgendaItemKey}"
        }, cancellationToken);
    }
}
