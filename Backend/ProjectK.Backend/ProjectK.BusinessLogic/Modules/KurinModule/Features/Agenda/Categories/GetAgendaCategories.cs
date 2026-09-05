using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Categories;

/// <summary>Event groups of a kurin. Archived ones are included only for the management page.</summary>
public sealed record GetAgendaCategories(Guid KurinKey, bool IncludeArchived)
    : IRequest<ServiceResult<IEnumerable<AgendaCategoryResponse>>>;

public sealed class GetAgendaCategoriesHandler
    : IRequestHandler<GetAgendaCategories, ServiceResult<IEnumerable<AgendaCategoryResponse>>>
{
    private readonly IUnitOfWork _uow;

    public GetAgendaCategoriesHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ServiceResult<IEnumerable<AgendaCategoryResponse>>> Handle(GetAgendaCategories request, CancellationToken cancellationToken)
    {
        var categories = await _uow.AgendaCategories.GetForKurinAsync(request.KurinKey, request.IncludeArchived, cancellationToken);
        var responses = categories.Select(AgendaCategoryResponse.From).ToList();
        return new ServiceResult<IEnumerable<AgendaCategoryResponse>>(ResultType.Success, responses);
    }
}
