using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Update;

public sealed record UpdateAgendaItem : IRequest<ServiceResult<object>>
{
    public Guid AgendaItemKey { get; init; }
    public AgendaItemKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
    public bool IsAllDay { get; init; } = true;
    public List<AgendaTargetInput> Targets { get; init; } = [];
}

public sealed class UpdateAgendaItemHandler : IRequestHandler<UpdateAgendaItem, ServiceResult<object>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;
    private readonly ICurrentUserContext _currentUser;
    private readonly INotificationService _notifications;

    public UpdateAgendaItemHandler(
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

    public async Task<ServiceResult<object>> Handle(UpdateAgendaItem request, CancellationToken cancellationToken)
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

        if (!AgendaPermissions.CanManage(item, viewer))
        {
            return ServiceResult<object>.Failure(ResultType.Forbidden, "AGENDA_EDIT_FORBIDDEN", "Only the creator or leadership may edit this item.");
        }

        foreach (var target in request.Targets)
        {
            var decision = await _access.AuthorizeTargetAsync(target, ResourceAction.Create, cancellationToken);
            if (!decision.IsAllowed)
            {
                return ServiceResult<object>.Failure(ResultType.Forbidden, "AGENDA_TARGET_FORBIDDEN", decision.Reason);
            }
        }

        item.Kind = request.Kind;
        item.Title = request.Title.Trim();
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        item.StartUtc = request.StartUtc;
        item.EndUtc = request.EndUtc;
        item.IsAllDay = request.IsAllDay;
        item.UpdatedDate = DateTime.UtcNow;

        // Reconcile targets in place: keep unchanged rows, drop removed ones, add new ones. Replacing
        // the whole set (clear + re-add) would delete and re-insert identical rows in one SaveChanges,
        // which trips the unique (AgendaItemKey, TargetType, TargetKey) index. The item is tracked, so
        // these collection edits are persisted without an explicit Update() call.
        var desired = request.Targets
            .Select(t => (t.TargetType, t.TargetKey))
            .ToHashSet();

        foreach (var existing in item.Assignments.ToList())
        {
            if (!desired.Contains((existing.TargetType, existing.TargetKey)))
            {
                item.Assignments.Remove(existing);
            }
        }

        var current = item.Assignments
            .Select(a => (a.TargetType, a.TargetKey))
            .ToHashSet();

        foreach (var target in request.Targets)
        {
            if (current.Add((target.TargetType, target.TargetKey)))
            {
                item.Assignments.Add(new AgendaAssignment { TargetType = target.TargetType, TargetKey = target.TargetKey });
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        await NotifyUpdatedAsync(item, viewer.ViewerUserKey ?? Guid.Empty, cancellationToken);

        return new ServiceResult<object>(ResultType.Success);
    }

    private async Task NotifyUpdatedAsync(AgendaItem item, Guid actorUserKey, CancellationToken cancellationToken)
    {
        var recipients = await AgendaNotificationRecipients.ResolveAsync(_uow, item, actorUserKey, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var requests = recipients.Select(userKey => new NotificationRequest
        {
            RecipientUserKey = userKey,
            Type = AppNotificationType.AgendaItemUpdated,
            Severity = AppNotificationSeverity.Info,
            Title = "Оновлено призначення",
            Body = item.Title,
            EntityType = "AgendaItem",
            EntityKey = item.AgendaItemKey,
            Route = AgendaRoutes.For(item),
            ActorUserKey = actorUserKey,
            DeduplicationKey = $"agenda-updated:{item.AgendaItemKey}:{userKey}"
        });

        await _notifications.NotifyManyAsync(requests, cancellationToken);
    }
}
