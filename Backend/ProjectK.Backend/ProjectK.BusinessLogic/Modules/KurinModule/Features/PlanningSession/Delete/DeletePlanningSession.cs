using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Delete;

public record DeletePlanningSession(Guid planningSessionKey) : IRequest<ServiceResult<object>>;


public class DeletePlanningSessionHandler : IRequestHandler<DeletePlanningSession, ServiceResult<object>>
{
    private readonly IUnitOfWork _uow;
    public DeletePlanningSessionHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }
    public async Task<ServiceResult<object>> Handle(DeletePlanningSession request, CancellationToken cancellationToken)
    {
        var session = await _uow.PlanningSessions.GetByKeyAsync(request.planningSessionKey, cancellationToken);
        if (session == null)
        {
            return new ServiceResult<object>(Common.Models.Enums.ResultType.NotFound, "Planning session not found.");
        }
        _uow.PlanningSessions.Delete(session, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new ServiceResult<object>(Common.Models.Enums.ResultType.Success, "Planning deleted successfully");
    }
}