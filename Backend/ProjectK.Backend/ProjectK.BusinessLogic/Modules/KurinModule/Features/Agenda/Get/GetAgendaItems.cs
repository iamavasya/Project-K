using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.AuthModule;
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
    private readonly UserManager<AppUser> _userManager;
    private readonly TimeProvider _timeProvider;

    public GetAgendaItemsHandler(IUnitOfWork uow, IAgendaAccess access, UserManager<AppUser> userManager, TimeProvider timeProvider)
    {
        _uow = uow;
        _access = access;
        _userManager = userManager;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceResult<IEnumerable<AgendaItemResponse>>> Handle(GetAgendaItems request, CancellationToken cancellationToken)
    {
        var viewer = await _access.BuildViewerAsync(request.KurinKey, cancellationToken);

        var items = (await _uow.AgendaItems.GetForViewerAsync(
            viewer.ToScope(),
            request.FromUtc,
            request.ToUtc,
            onlyDated: true,
            kind: null,
            cancellationToken)).ToList();

        var lookups = await AgendaLookups.LoadAsync(_uow, request.KurinKey, cancellationToken);
        var creatorNames = await AgendaCreatorNames.ResolveAsync(_userManager, lookups.CreatorNames, items, cancellationToken);

        // Recurring items are expanded into one row per occurrence inside the query window; one-offs pass
        // through unchanged. A missing window is bounded so an open-ended series can't expand forever.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var windowFrom = request.FromUtc ?? now.AddMonths(-6);
        var windowTo = request.ToUtc ?? now.AddMonths(12);

        var responses = items
            .SelectMany(item => item.RecurrenceFrequency == RecurrenceFrequency.None
                ? new[] { AgendaItemResponseFactory.Create(item, viewer, AgendaLookups.KurinLabel, lookups.GroupNames, lookups.MemberNames, creatorNames, lookups.LeadershipLabels, lookups.Categories) }
                : AgendaRecurrence.Expand(item, windowFrom, windowTo)
                    .Select(occ => AgendaItemResponseFactory.Create(item, viewer, AgendaLookups.KurinLabel, lookups.GroupNames, lookups.MemberNames, creatorNames, lookups.LeadershipLabels, lookups.Categories, occ.StartUtc, occ.EndUtc)))
            .OrderBy(r => r.StartUtc)
            .ToList();

        return new ServiceResult<IEnumerable<AgendaItemResponse>>(ResultType.Success, responses);
    }
}
