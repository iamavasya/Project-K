using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Create;

public sealed record CreateAgendaItem : IRequest<ServiceResult<Guid>>
{
    public Guid KurinKey { get; init; }
    public AgendaItemKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? StartUtc { get; init; }
    public DateTime? EndUtc { get; init; }
    public bool IsAllDay { get; init; } = true;
    public Guid? AgendaCategoryKey { get; init; }
    public RecurrenceFrequency RecurrenceFrequency { get; init; } = RecurrenceFrequency.None;
    public int RecurrenceInterval { get; init; } = 1;
    public int RecurrenceByWeekday { get; init; }
    public DateTime? RecurrenceEndUtc { get; init; }
    public int? RecurrenceCount { get; init; }
    public List<AgendaTargetInput> Targets { get; init; } = [];
}

public sealed class CreateAgendaItemHandler : IRequestHandler<CreateAgendaItem, ServiceResult<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;
    private readonly ICurrentUserContext _currentUser;
    private readonly INotificationService _notifications;

    public CreateAgendaItemHandler(
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

    public async Task<ServiceResult<Guid>> Handle(CreateAgendaItem request, CancellationToken cancellationToken)
    {
        var actorUserKey = _currentUser.UserId;
        if (actorUserKey is null)
        {
            return ServiceResult<Guid>.Failure(ResultType.Unauthorized, "AGENDA_NO_ACTOR", "Current user could not be resolved.");
        }

        // Every selected target is checked against the tested resource guard, so a mentor/group leader
        // cannot reach past their assigned groups even if the client sends a wider selection.
        foreach (var target in request.Targets)
        {
            var decision = await _access.AuthorizeTargetAsync(target, ResourceAction.Create, cancellationToken);
            if (!decision.IsAllowed)
            {
                return ServiceResult<Guid>.Failure(ResultType.Forbidden, "AGENDA_TARGET_FORBIDDEN", decision.Reason);
            }
        }

        var item = new AgendaItem
        {
            KurinKey = request.KurinKey,
            Kind = request.Kind,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = AgendaItemStatus.Todo,
            StartUtc = request.StartUtc,
            EndUtc = request.EndUtc,
            IsAllDay = request.IsAllDay,
            AgendaCategoryKey = request.AgendaCategoryKey,
            RecurrenceFrequency = request.RecurrenceFrequency,
            RecurrenceInterval = Math.Max(1, request.RecurrenceInterval),
            RecurrenceByWeekday = request.RecurrenceByWeekday,
            RecurrenceEndUtc = request.RecurrenceEndUtc,
            RecurrenceCount = request.RecurrenceCount,
            CreatedByUserKey = actorUserKey.Value,
            Assignments = request.Targets
                .Select(t => new AgendaAssignment { TargetType = t.TargetType, TargetKey = t.TargetKey })
                .ToList()
        };

        _uow.AgendaItems.Create(item, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await NotifyAssignedAsync(item, actorUserKey.Value, cancellationToken);

        return new ServiceResult<Guid>(ResultType.Created, item.AgendaItemKey);
    }

    private async Task NotifyAssignedAsync(AgendaItem item, Guid actorUserKey, CancellationToken cancellationToken)
    {
        var recipients = await AgendaNotificationRecipients.ResolveAsync(_uow, item, actorUserKey, cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var kindWord = item.Kind == AgendaItemKind.Task ? "Задачу" : "Подію";
        var requests = recipients.Select(userKey => new NotificationRequest
        {
            RecipientUserKey = userKey,
            Type = AppNotificationType.AgendaItemAssigned,
            Severity = AppNotificationSeverity.Info,
            Title = $"{kindWord} призначено вам",
            Body = item.Title,
            EntityType = "AgendaItem",
            EntityKey = item.AgendaItemKey,
            Route = AgendaRoutes.For(item),
            ActorUserKey = actorUserKey,
            DeduplicationKey = $"agenda-assigned:{item.AgendaItemKey}:{userKey}"
        });

        await _notifications.NotifyManyAsync(requests, cancellationToken);
    }
}
