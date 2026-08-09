using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Get;

/// <summary>Board feed: every task the current user may see, dated or not, for the kanban columns.</summary>
public sealed record GetAgendaBoard(Guid KurinKey)
    : IRequest<ServiceResult<IEnumerable<AgendaItemResponse>>>;

public sealed class GetAgendaBoardHandler
    : IRequestHandler<GetAgendaBoard, ServiceResult<IEnumerable<AgendaItemResponse>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;

    public GetAgendaBoardHandler(IUnitOfWork uow, IAgendaAccess access)
    {
        _uow = uow;
        _access = access;
    }

    public async Task<ServiceResult<IEnumerable<AgendaItemResponse>>> Handle(GetAgendaBoard request, CancellationToken cancellationToken)
    {
        var viewer = await _access.BuildViewerAsync(request.KurinKey, cancellationToken);

        var items = await _uow.AgendaItems.GetForViewerAsync(
            viewer.ToScope(),
            fromUtc: null,
            toUtc: null,
            onlyDated: false,
            kind: AgendaItemKind.Task,
            cancellationToken);

        var lookups = await AgendaLookups.LoadAsync(_uow, request.KurinKey, cancellationToken);

        var responses = items
            .Select(item => AgendaItemResponseFactory.Create(item, viewer, AgendaLookups.KurinLabel, lookups.GroupNames, lookups.MemberNames))
            .ToList();

        return new ServiceResult<IEnumerable<AgendaItemResponse>>(ResultType.Success, responses);
    }
}
