using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Responses;

/// <summary>Who answered an event's invitation, with confirmed/waitlist ranking and the caller's own choice.</summary>
public sealed record GetAgendaResponses(Guid AgendaItemKey) : IRequest<ServiceResult<AgendaResponsesResponse>>;

public sealed class GetAgendaResponsesHandler : IRequestHandler<GetAgendaResponses, ServiceResult<AgendaResponsesResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;
    private readonly ICurrentUserContext _currentUser;

    public GetAgendaResponsesHandler(IUnitOfWork uow, IAgendaAccess access, ICurrentUserContext currentUser)
    {
        _uow = uow;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<AgendaResponsesResponse>> Handle(GetAgendaResponses request, CancellationToken cancellationToken)
    {
        var item = await _uow.AgendaItems.GetByKeyWithAssignmentsAsync(request.AgendaItemKey, cancellationToken);
        if (item is null)
        {
            return ServiceResult<AgendaResponsesResponse>.Failure(ResultType.NotFound, "AGENDA_NOT_FOUND", "Agenda item was not found.");
        }

        if (item.Kind != AgendaItemKind.Event)
        {
            return ServiceResult<AgendaResponsesResponse>.Failure(ResultType.BadRequest, "AGENDA_NOT_EVENT", "Only events accept RSVPs.");
        }

        var viewer = await _access.BuildViewerAsync(item.KurinKey, cancellationToken);
        if (!AgendaPermissions.IsVisibleTo(item, viewer))
        {
            return ServiceResult<AgendaResponsesResponse>.Failure(ResultType.Forbidden, "AGENDA_NOT_VISIBLE", "You cannot view this event.");
        }

        var responses = await _uow.AgendaResponses.GetForItemAsync(item.AgendaItemKey, cancellationToken);
        var names = await ResolveNamesAsync(item.KurinKey, cancellationToken);

        int? capacity = null;
        var waitlistEnabled = false;
        if (item.AgendaCategoryKey.HasValue)
        {
            var category = await _uow.AgendaCategories.GetByKeyAsync(item.AgendaCategoryKey.Value, cancellationToken);
            capacity = category?.Capacity;
            waitlistEnabled = category?.WaitlistEnabled ?? false;
        }

        var picture = AgendaRsvpProjector.Project(item.AgendaItemKey, responses, capacity, waitlistEnabled, names, _currentUser.UserId);
        return new ServiceResult<AgendaResponsesResponse>(ResultType.Success, picture);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(Guid kurinKey, CancellationToken cancellationToken)
    {
        var members = await _uow.Members.GetAllByKurinKeyAsync(kurinKey, cancellationToken);
        return members
            .Where(m => m.UserKey.HasValue && m.UserKey.Value != Guid.Empty)
            .GroupBy(m => m.UserKey!.Value)
            .ToDictionary(g => g.Key, g => $"{g.First().FirstName} {g.First().LastName}".Trim());
    }
}
