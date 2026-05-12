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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Get;

public record GetPlanningSessions(Guid kurinKey) : IRequest<ServiceResult<IEnumerable<PlanningSessionResponse>>>;

public class GetPlanningSessionsHandler : IRequestHandler<GetPlanningSessions, ServiceResult<IEnumerable<PlanningSessionResponse>>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    public GetPlanningSessionsHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }
    public async Task<ServiceResult<IEnumerable<PlanningSessionResponse>>> Handle(GetPlanningSessions request, CancellationToken cancellationToken)
    {
        var entities = await _uow.PlanningSessions.GetAllByKurinKeyAsync(request.kurinKey, cancellationToken);
        var dtos = _mapper.Map<IEnumerable<PlanningSessionResponse>>(entities);
        return new ServiceResult<IEnumerable<PlanningSessionResponse>>(ResultType.Success, dtos);
    }
}