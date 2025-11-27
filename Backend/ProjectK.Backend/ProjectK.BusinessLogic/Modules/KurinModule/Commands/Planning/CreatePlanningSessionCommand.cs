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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Commands.Planning;

public class CreatePlanningSessionCommand : IRequest<ServiceResult<Guid>>
{
    public string Name { get; set; } = string.Empty;
    public Guid KurinKey { get; set; }
    public DateTime SearchStart { get; set; }
    public DateTime SearchEnd { get; set; }
    public int DurationDays { get; set; }
    public List<ParticipantInputDto> Participants { get; set; } = [];
}

public class CreatePlanningSessionCommandHandler : IRequestHandler<CreatePlanningSessionCommand, ServiceResult<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly IOptimizer _optimizer;
    public CreatePlanningSessionCommandHandler(IUnitOfWork uow, IOptimizer optimizer)
    {
        _uow = uow;
        _optimizer = optimizer;
    }

    public async Task<ServiceResult<Guid>> Handle(CreatePlanningSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new PlanningSession
        {
            Name = request.Name,
            KurinKey = request.KurinKey,
            SearchStart = request.SearchStart,
            SearchEnd = request.SearchEnd,
            DurationDays = request.DurationDays,
            IsCalculated = false,
        };

        foreach (var participantDto in request.Participants)
        {
            var participant = new PlanningParticipant
            {
                MemberKey = participantDto.MemberKey,
                FullName = participantDto.FullName,
                RoleWeight = participantDto.RoleWeight,
                PlanningSession = session
            };
            
            foreach (var rDto in participantDto.BusyRanges)
            {
                var busyRange = new ParticipantBusyRange
                {
                    Start = rDto.Start,
                    End = rDto.End,
                };
                participant.BusyRanges.Add(busyRange);
            }
        }

        // _uow.PlanningSessionsRepository.Add(session);
        // await _uow.SaveChangesAsync(cancellationToken);

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

        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<Guid>(ResultType.Success, session.PlanningSessionKey);
    }
}
