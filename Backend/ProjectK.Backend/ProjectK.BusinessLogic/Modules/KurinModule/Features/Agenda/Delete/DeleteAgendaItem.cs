using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Events;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Dtos.InfrastructureModule;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Delete;

public sealed record DeleteAgendaItem(Guid AgendaItemKey) : IRequest<ServiceResult<object>>;

public sealed class DeleteAgendaItemHandler : IRequestHandler<DeleteAgendaItem, ServiceResult<object>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMemberDirectory _members;
    private readonly IAgendaAccess _access;
    private readonly ICurrentUserContext _currentUser;
    private readonly IDomainEventPublisher _events;

    public DeleteAgendaItemHandler(
        IUnitOfWork uow,
        IMemberDirectory members,
        IAgendaAccess access,
        ICurrentUserContext currentUser,
        IDomainEventPublisher events)
    {
        _uow = uow;
        _members = members;
        _access = access;
        _currentUser = currentUser;
        _events = events;
    }

    public async Task<ServiceResult<object>> Handle(DeleteAgendaItem request, CancellationToken cancellationToken)
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
            return ServiceResult<object>.Failure(ResultType.Forbidden, "AGENDA_DELETE_FORBIDDEN", "Only the creator or leadership may delete this item.");
        }

        // Resolve who was assigned before the row (and its assignments) are gone.
        var actorUserKey = viewer.ViewerUserKey ?? Guid.Empty;
        var recipients = await AgendaNotificationRecipients.ResolveAsync(_uow, _members, item, actorUserKey, cancellationToken);
        var title = item.Title;
        var kind = item.Kind;
        var kurinKey = item.KurinKey;

        // Assignments are removed by the cascade delete configured on the relationship.
        _uow.AgendaItems.Delete(item, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _events.PublishAsync(
            new AgendaItemRemoved(
                request.AgendaItemKey,
                kurinKey,
                kind,
                title,
                recipients,
                actorUserKey),
            cancellationToken);

        return new ServiceResult<object>(ResultType.Success);
    }
}
