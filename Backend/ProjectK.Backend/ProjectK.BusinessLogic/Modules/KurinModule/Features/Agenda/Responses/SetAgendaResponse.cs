using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Responses;

/// <summary>The caller's RSVP to an event (йду/не йду/можливо). Upserts one row per (event, user).</summary>
public sealed record SetAgendaResponse(Guid AgendaItemKey, AgendaRsvpStatus Status)
    : IRequest<ServiceResult<AgendaResponsesResponse>>;

public sealed class SetAgendaResponseHandler : IRequestHandler<SetAgendaResponse, ServiceResult<AgendaResponsesResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;
    private readonly ICurrentUserContext _currentUser;

    public SetAgendaResponseHandler(IUnitOfWork uow, IAgendaAccess access, ICurrentUserContext currentUser)
    {
        _uow = uow;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<AgendaResponsesResponse>> Handle(SetAgendaResponse request, CancellationToken cancellationToken)
    {
        var userKey = _currentUser.UserId;
        if (userKey is null)
        {
            return ServiceResult<AgendaResponsesResponse>.Failure(ResultType.Unauthorized, "AGENDA_NO_ACTOR", "Current user could not be resolved.");
        }

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
            return ServiceResult<AgendaResponsesResponse>.Failure(ResultType.Forbidden, "AGENDA_NOT_VISIBLE", "You cannot respond to this event.");
        }

        var existing = await _uow.AgendaResponses.GetForItemAndUserAsync(item.AgendaItemKey, userKey.Value, cancellationToken);
        if (existing is null)
        {
            _uow.AgendaResponses.Create(new AgendaResponse
            {
                AgendaItemKey = item.AgendaItemKey,
                UserKey = userKey.Value,
                Status = request.Status,
                RespondedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        else if (existing.Status != request.Status)
        {
            // Re-time on change so switching to «Going» joins the back of the queue rather than keeping
            // an earlier «Maybe» slot.
            existing.Status = request.Status;
            existing.RespondedAtUtc = DateTime.UtcNow;
            existing.UpdatedDate = DateTime.UtcNow;
            _uow.AgendaResponses.Update(existing, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

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

        var picture = AgendaRsvpProjector.Project(item.AgendaItemKey, responses, capacity, waitlistEnabled, names, userKey);
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
