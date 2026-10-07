using FluentValidation;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.BusinessLogic.Modules.MeModule.Models;
using ProjectK.BusinessLogic.Modules.MeModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.MeModule.Features.Events;

/// <summary>What is ahead for the person in the next days, across every kurin they stand in.</summary>
public sealed record GetMyEventsQuery(int Days) : IRequest<ServiceResult<IReadOnlyList<MyEventDto>>>;

public sealed class GetMyEventsQueryValidator : AbstractValidator<GetMyEventsQuery>
{
    public GetMyEventsQueryValidator()
    {
        RuleFor(q => q.Days).InclusiveBetween(1, 90);
    }
}

public sealed class GetMyEventsQueryHandler : IRequestHandler<GetMyEventsQuery, ServiceResult<IReadOnlyList<MyEventDto>>>
{
    private readonly MeAgendaScopes _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly TimeProvider _time;

    public GetMyEventsQueryHandler(MeAgendaScopes scopes, IUnitOfWork unitOfWork, ICurrentUserContext currentUser, TimeProvider time)
    {
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ServiceResult<IReadOnlyList<MyEventDto>>> Handle(GetMyEventsQuery request, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var until = now.AddDays(request.Days);
        var rows = new List<MyEventDto>();

        foreach (var context in await _scopes.BuildAsync(cancellationToken))
        {
            var kurin = MeKurins.Ref(context.Membership, _currentUser.KurinKey);
            var items = await _unitOfWork.AgendaItems.GetForViewerAsync(
                context.Viewer.ToScope(), now, until, onlyDated: true, kind: AgendaItemKind.Event, cancellationToken);
            var categories = (await _unitOfWork.AgendaCategories.GetForKurinAsync(context.Membership.KurinKey, includeArchived: true, cancellationToken))
                .ToDictionary(c => c.AgendaCategoryKey);

            foreach (var item in items)
            {
                var response = await _unitOfWork.AgendaResponses.GetForItemAndUserAsync(item.AgendaItemKey, context.Viewer.ViewerUserKey!.Value, cancellationToken);
                var category = item.AgendaCategoryKey is { } key ? categories.GetValueOrDefault(key) : null;

                foreach (var occurrence in AgendaRecurrence.Expand(item, now, until))
                {
                    rows.Add(ToDto(item, kurin, category, occurrence, response?.Status));
                }
            }
        }

        return new ServiceResult<IReadOnlyList<MyEventDto>>(ResultType.Success, rows.OrderBy(r => r.StartUtc).ThenBy(r => r.Title).ToList());
    }

    private static MyEventDto ToDto(AgendaItem item, MyKurinRefDto kurin, AgendaCategory? category, AgendaOccurrence occurrence, AgendaRsvpStatus? response) => new()
    {
        AgendaItemKey = item.AgendaItemKey,
        Kurin = kurin,
        Title = item.Title,
        StartUtc = DateTime.SpecifyKind(occurrence.StartUtc, DateTimeKind.Utc),
        EndUtc = occurrence.EndUtc is { } end ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : null,
        IsAllDay = item.IsAllDay,
        IsRecurring = item.RecurrenceFrequency != RecurrenceFrequency.None,
        CategoryName = category?.Name,
        CategoryColorHex = category?.ColorHex,
        CategoryIcon = category?.Icon,
        RsvpRequired = category?.RsvpRequired ?? false,
        MyResponse = response
    };
}
