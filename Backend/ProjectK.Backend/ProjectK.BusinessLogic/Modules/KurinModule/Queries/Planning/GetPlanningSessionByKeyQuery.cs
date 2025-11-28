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

public record GetPlanningSessionByKeyQuery(Guid entityKey) : IRequest<ServiceResult<PlanningSessionDto>>;

public class GetPlanningSessionHandler : IRequestHandler<GetPlanningSessionByKeyQuery, ServiceResult<PlanningSessionDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetPlanningSessionHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PlanningSessionDto>> Handle(GetPlanningSessionByKeyQuery request, CancellationToken cancellationToken)
    {
        var entity = await _uow.PlanningSessions.GetByKeyWithDetailsAsync(request.entityKey);

        if (entity == null)
        {
            return new ServiceResult<PlanningSessionDto>(ResultType.NotFound);
        }

        var dto = _mapper.Map<PlanningSessionDto>(entity);
        return new ServiceResult<PlanningSessionDto>(ResultType.Success, dto);
    }
}
