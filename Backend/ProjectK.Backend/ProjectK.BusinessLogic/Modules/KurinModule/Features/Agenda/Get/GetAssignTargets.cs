using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Get;

/// <summary>The "Assign for" tree, trimmed to what the current user is allowed to target.</summary>
public sealed record GetAssignTargets(Guid KurinKey)
    : IRequest<ServiceResult<AgendaAssignTargetsResponse>>;

public sealed class GetAssignTargetsHandler
    : IRequestHandler<GetAssignTargets, ServiceResult<AgendaAssignTargetsResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAgendaAccess _access;

    public GetAssignTargetsHandler(IUnitOfWork uow, IAgendaAccess access)
    {
        _uow = uow;
        _access = access;
    }

    public async Task<ServiceResult<AgendaAssignTargetsResponse>> Handle(GetAssignTargets request, CancellationToken cancellationToken)
    {
        var viewer = await _access.BuildViewerAsync(request.KurinKey, cancellationToken);

        var kurinDecision = await _access.AuthorizeTargetAsync(
            new AgendaTargetInput { TargetType = AgendaTargetType.Kurin, TargetKey = request.KurinKey },
            ResourceAction.Create,
            cancellationToken);

        var groups = (await _uow.Groups.GetAllAsync(request.KurinKey, cancellationToken)).ToList();
        var members = (await _uow.Members.GetAllByKurinKeyAsync(request.KurinKey, cancellationToken)).ToList();

        // Managers/admins reach every group; mentors/group leaders only their scoped groups.
        var visibleGroups = viewer.CanSeeWholeKurin
            ? groups
            : groups.Where(g => viewer.VisibilityGroupKeys.Contains(g.GroupKey)).ToList();

        var response = new AgendaAssignTargetsResponse
        {
            CanTargetKurin = kurinDecision.IsAllowed,
            KurinKey = request.KurinKey,
            KurinLabel = AgendaLookups.KurinLabel,
            Groups = visibleGroups
                .OrderBy(g => g.Name)
                .Select(g => new AgendaGroupTargetDto
                {
                    GroupKey = g.GroupKey,
                    Name = g.Name,
                    CanTargetGroup = true,
                    Members = members
                        .Where(m => m.GroupKey == g.GroupKey)
                        .OrderBy(m => m.LastName)
                        .Select(m => new AgendaMemberTargetDto
                        {
                            MemberKey = m.MemberKey,
                            FullName = $"{m.FirstName} {m.LastName}".Trim()
                        })
                        .ToList()
                })
                .ToList()
        };

        return new ServiceResult<AgendaAssignTargetsResponse>(ResultType.Success, response);
    }
}
