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

public record GetPlanningSessionByKey(Guid entityKey) : IRequest<ServiceResult<PlanningSessionResponse>>;

public class GetPlanningSessionHandler : IRequestHandler<GetPlanningSessionByKey, ServiceResult<PlanningSessionResponse>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserContext _currentUser;

    public GetPlanningSessionHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserContext currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<PlanningSessionResponse>> Handle(GetPlanningSessionByKey request, CancellationToken cancellationToken)
    {
        var entity = await _uow.PlanningSessions.GetByKeyWithDetailsAsync(request.entityKey);

        if (entity == null)
        {
            return new ServiceResult<PlanningSessionResponse>(ResultType.NotFound);
        }

        var dto = _mapper.Map<PlanningSessionResponse>(entity);
        dto.CanDelete = PlanningSessionAccess.CanDelete(_currentUser, entity.CreatedByUserKey);
        return new ServiceResult<PlanningSessionResponse>(ResultType.Success, dto);
    }
}
