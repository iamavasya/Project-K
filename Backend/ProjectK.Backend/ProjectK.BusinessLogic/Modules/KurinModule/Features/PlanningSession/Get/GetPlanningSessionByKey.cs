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

public record GetPlanningSessionByKey(Guid entityKey) : IRequest<ServiceResult<PlanningSessionResponse>>;

public class GetPlanningSessionHandler : IRequestHandler<GetPlanningSessionByKey, ServiceResult<PlanningSessionResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetPlanningSessionHandler(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PlanningSessionResponse>> Handle(GetPlanningSessionByKey request, CancellationToken cancellationToken)
    {
        var entity = await _uow.PlanningSessions.GetByKeyWithDetailsAsync(request.entityKey);

        if (entity == null)
        {
            return new ServiceResult<PlanningSessionResponse>(ResultType.NotFound);
        }

        var dto = _mapper.Map<PlanningSessionResponse>(entity);
        return new ServiceResult<PlanningSessionResponse>(ResultType.Success, dto);
    }
}
