using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Get;

/// <summary>Calendar feed: dated items in the window that the current user may see.</summary>
public sealed record GetAgendaItems(Guid KurinKey, DateTime? FromUtc, DateTime? ToUtc)
    : IRequest<ServiceResult<IEnumerable<AgendaItemResponse>>>;

public sealed class GetAgendaItemsHandler
    : IRequestHandler<GetAgendaItems, ServiceResult<IEnumerable<AgendaItemResponse>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;

    public GetAgendaItemsHandler(IUnitOfWork uow, IAgendaAccess access)
    {
        _uow = uow;
        _access = access;
    }

    public async Task<ServiceResult<IEnumerable<AgendaItemResponse>>> Handle(GetAgendaItems request, CancellationToken cancellationToken)
    {
        var viewer = await _access.BuildViewerAsync(request.KurinKey, cancellationToken);

        var items = await _uow.AgendaItems.GetForViewerAsync(
            viewer.ToScope(),
            request.FromUtc,
            request.ToUtc,
            onlyDated: true,
            kind: null,
            cancellationToken);

        var lookups = await AgendaLookups.LoadAsync(_uow, request.KurinKey, cancellationToken);

        var responses = items
            .Select(item => AgendaItemResponseFactory.Create(item, viewer, AgendaLookups.KurinLabel, lookups.GroupNames, lookups.MemberNames))
            .ToList();

        return new ServiceResult<IEnumerable<AgendaItemResponse>>(ResultType.Success, responses);
    }
}
