using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Delete;

public sealed record DeleteAgendaItem(Guid AgendaItemKey) : IRequest<ServiceResult<object>>;

public sealed class DeleteAgendaItemHandler : IRequestHandler<DeleteAgendaItem, ServiceResult<object>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;
    private readonly ICurrentUserContext _currentUser;

    public DeleteAgendaItemHandler(IUnitOfWork uow, IAgendaAccess access, ICurrentUserContext currentUser)
    {
        _uow = uow;
        _access = access;
        _currentUser = currentUser;
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

        // Assignments are removed by the cascade delete configured on the relationship.
        _uow.AgendaItems.Delete(item, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<object>(ResultType.Success);
    }
}
