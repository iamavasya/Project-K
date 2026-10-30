using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Project;

/// <summary>The projects of a person's third probe, and whether the viewer may write them.</summary>
public sealed record GetProbeProjectsQuery(Guid MemberKey, string ProbeId) : IRequest<ServiceResult<ProbeProjectsResponse>>;

public sealed class GetProbeProjectsQueryHandler : IRequestHandler<GetProbeProjectsQuery, ServiceResult<ProbeProjectsResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProbeProjectAccess _access;
    private readonly IBlobReadLinks _links;

    public GetProbeProjectsQueryHandler(IUnitOfWork unitOfWork, IProbeProjectAccess access, IBlobReadLinks links)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _links = links;
    }

    public async Task<ServiceResult<ProbeProjectsResponse>> Handle(GetProbeProjectsQuery request, CancellationToken cancellationToken)
    {
        var probeId = request.ProbeId.Trim();
        if (!await _access.IsProjectProbeAsync(request.MemberKey, probeId, cancellationToken))
        {
            return new ServiceResult<ProbeProjectsResponse>(
                ResultType.Success,
                new ProbeProjectsResponse(false, false, [], ProbeProjectMapping.Limits));
        }

        var projects = await _unitOfWork.ProbeProjects.GetForMemberProbeAsync(request.MemberKey, probeId, cancellationToken);
        var canEdit = await _access.CanEditAsync(request.MemberKey, cancellationToken);

        return new ServiceResult<ProbeProjectsResponse>(
            ResultType.Success,
            new ProbeProjectsResponse(
                true,
                canEdit,
                projects.Select(project => ProbeProjectMapping.ToResponse(project, _links)).ToList(),
                ProbeProjectMapping.Limits));
    }
}
