using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Solvers;
using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Optimization.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlanningSessionEntity = ProjectK.Common.Entities.KurinModule.Planning.PlanningSession;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Create;

public class CreatePlanningSession : IRequest<ServiceResult<Guid>>
{
    public string Name { get; set; } = string.Empty;
    public Guid KurinKey { get; set; }
    public DateTime SearchStart { get; set; }
    public DateTime SearchEnd { get; set; }
    public int DurationDays { get; set; }
    public List<ParticipantInputDto> Participants { get; set; } = [];
}

public class CreatePlanningSessionHandler : IRequestHandler<CreatePlanningSession, ServiceResult<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly IOptimizer _optimizer;
    private readonly IMapper _mapper;
    public CreatePlanningSessionHandler(IUnitOfWork uow, IOptimizer optimizer, IMapper mapper)
    {
        _uow = uow;
        _optimizer = optimizer;
        _mapper = mapper;
    }

    public async Task<ServiceResult<Guid>> Handle(CreatePlanningSession request, CancellationToken cancellationToken)
    {
        var session = _mapper.Map<PlanningSessionEntity>(request);

        var problem = new CampDateSolver(
            session.SearchStart,
            session.SearchEnd,
            session.DurationDays,
            session.Participants.ToList()
        );

        var result = _optimizer.Solve(problem, wolves: 40, iterations: 100);

        var bestStartDate = problem.PositionToDate(result.BestPosition);

        session.OptimalStartDate = bestStartDate;
        session.OptimalEndDate = bestStartDate.AddDays(session.DurationDays);
        session.ConflictScore = result.BestFitness;
        session.IsCalculated = true;

        _uow.PlanningSessions.Create(session, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<Guid>(ResultType.Success, session.PlanningSessionKey);
    }
}
