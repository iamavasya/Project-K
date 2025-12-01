using AutoMapper;
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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Queries.Planning;

public record GetPlanningSessionsQuery(Guid kurinKey) : IRequest<ServiceResult<IEnumerable<PlanningSessionDto>>>;

public class GetPlanningSessionsQueryHandler : IRequestHandler<GetPlanningSessionsQuery, ServiceResult<IEnumerable<PlanningSessionDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    public GetPlanningSessionsQueryHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }
    public async Task<ServiceResult<IEnumerable<PlanningSessionDto>>> Handle(GetPlanningSessionsQuery request, CancellationToken cancellationToken)
    {
        var entities = await _uow.PlanningSessions.GetAllByKurinKeyAsync(request.kurinKey, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<PlanningSessionDto>>(entities);
        return new ServiceResult<IEnumerable<PlanningSessionDto>>(ResultType.Success, dtos);
    }
}