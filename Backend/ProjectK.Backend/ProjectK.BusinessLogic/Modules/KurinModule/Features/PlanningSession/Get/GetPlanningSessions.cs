using AutoMapper;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Get;

public record GetPlanningSessions(Guid kurinKey) : IRequest<ServiceResult<IEnumerable<PlanningSessionResponse>>>;

public class GetPlanningSessionsHandler : IRequestHandler<GetPlanningSessions, ServiceResult<IEnumerable<PlanningSessionResponse>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserContext _currentUser;

    public GetPlanningSessionsHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserContext currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }
    public async Task<ServiceResult<IEnumerable<PlanningSessionResponse>>> Handle(GetPlanningSessions request, CancellationToken cancellationToken)
    {
        var entities = (await _uow.PlanningSessions.GetAllByKurinKeyAsync(request.kurinKey, cancellationToken)).ToList();
        var dtos = _mapper.Map<List<PlanningSessionResponse>>(entities);

        foreach (var (dto, entity) in dtos.Zip(entities))
        {
            dto.CanDelete = PlanningSessionAccess.CanDelete(_currentUser, entity.CreatedByUserKey);
        }

        return new ServiceResult<IEnumerable<PlanningSessionResponse>>(ResultType.Success, dtos);
    }
}