using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.BusinessLogic.Modules.MeModule.Models;
using ProjectK.BusinessLogic.Modules.MeModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.MeModule.Features.Tasks;

/// <summary>
/// The person's open tasks across their kurins: what the board would show them, less what is done.
/// A status is movable from here only in the kurin the token acts in — the board's own endpoint does
/// the moving, and it answers for that kurin alone.
/// </summary>
public sealed record GetMyTasksQuery : IRequest<ServiceResult<IReadOnlyList<MyTaskDto>>>;

public sealed class GetMyTasksQueryHandler : IRequestHandler<GetMyTasksQuery, ServiceResult<IReadOnlyList<MyTaskDto>>>
{
    private readonly MeAgendaScopes _scopes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public GetMyTasksQueryHandler(MeAgendaScopes scopes, IUnitOfWork unitOfWork, ICurrentUserContext currentUser)
    {
        _scopes = scopes;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<IReadOnlyList<MyTaskDto>>> Handle(GetMyTasksQuery request, CancellationToken cancellationToken)
    {
        var rows = new List<MyTaskDto>();

        foreach (var context in await _scopes.BuildAsync(cancellationToken))
        {
            var kurin = MeKurins.Ref(context.Membership, _currentUser.KurinKey);
            var scope = context.Viewer.ToScope();
            var items = await _unitOfWork.AgendaItems.GetForViewerAsync(scope, null, null, onlyDated: false, kind: AgendaItemKind.Task, cancellationToken);

            rows.AddRange(items
                .Where(item => item.Status != AgendaItemStatus.Done)
                .Select(item => new MyTaskDto
                {
                    AgendaItemKey = item.AgendaItemKey,
                    Kurin = kurin,
                    Title = item.Title,
                    Status = item.Status,
                    StartUtc = item.StartUtc is { } start ? DateTime.SpecifyKind(start, DateTimeKind.Utc) : null,
                    EndUtc = item.EndUtc is { } end ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : null,
                    AddressedToMe = AgendaVisibility.IsAddressedTo(item, scope),
                    CanChangeStatus = kurin.IsCurrent && AgendaPermissions.CanChangeStatus(item, context.Viewer)
                }));
        }

        return new ServiceResult<IReadOnlyList<MyTaskDto>>(ResultType.Success, rows
            .OrderBy(r => r.EndUtc ?? r.StartUtc ?? DateTime.MaxValue)
            .ThenBy(r => r.Title)
            .ToList());
    }
}
