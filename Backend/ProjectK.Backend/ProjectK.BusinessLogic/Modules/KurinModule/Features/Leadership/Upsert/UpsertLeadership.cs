using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using LeadershipEntity = ProjectK.Common.Entities.KurinModule.Leadership;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Upsert
{
    public class UpsertLeadership : IRequest<ServiceResult<LeadershipResponse>>
    {
        public Guid? LeadershipKey { get; set; }
        public string? Type { get; set; }
        public Guid? EntityKey { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public IEnumerable<LeadershipHistoryMemberDto> LeadershipHistoryMembers { get; set; } = [];

        public UpsertLeadership(UpsertLeadershipRequest request)
        {
            Type = request.Type;
            EntityKey = request.EntityKey;
            StartDate = request.StartDate;
            EndDate = request.EndDate;
            LeadershipHistoryMembers = request.LeadershipHistories;
        }
        public UpsertLeadership(UpsertLeadershipRequest request, Guid leadershipKey)
        {
            LeadershipKey = leadershipKey;
            Type = request.Type;
            EntityKey = request.EntityKey;
            StartDate = request.StartDate;
            EndDate = request.EndDate;
            LeadershipHistoryMembers = request.LeadershipHistories;
        }
    }

    public class UpsertLeadershipHandler : IRequestHandler<UpsertLeadership, ServiceResult<LeadershipResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public UpsertLeadershipHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ServiceResult<LeadershipResponse>> Handle(UpsertLeadership request, CancellationToken cancellationToken)
        {
            LeadershipEntity? existing = null;
            bool isCreated = false;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (request.LeadershipKey != null && request.LeadershipKey != Guid.Empty)
            {
                existing = await _unitOfWork.Leaderships.GetByKeyAsync(request.LeadershipKey!.Value, cancellationToken);
            }

            if (existing != null)
            {
                // Update existing Leadership
                existing.Type = Enum.Parse<Common.Models.Enums.LeadershipType>(request.Type!, ignoreCase: true);
                switch (existing.Type)
                {
                    case LeadershipType.Kurin or LeadershipType.KV:
                        existing.KurinKey = request.EntityKey;
                        existing.GroupKey = null;
                        break;
                    case LeadershipType.Group:
                        existing.GroupKey = request.EntityKey;
                        existing.KurinKey = null;
                        break;
                }
                ApplyLeadershipHistoryChanges(existing, request.LeadershipHistoryMembers, today);
                _unitOfWork.Leaderships.Update(existing, cancellationToken);
            }
            else
            {
                // Create new Leadership
                existing = _mapper.Map<LeadershipEntity>(request);
                if (request.LeadershipKey is Guid leadershipKey && leadershipKey != Guid.Empty)
                {
                    existing.LeadershipKey = leadershipKey;
                }

                existing.StartDate = today;
                existing.EndDate = null;
                existing.Type = Enum.Parse<Common.Models.Enums.LeadershipType>(request.Type!, ignoreCase: true);
                switch (existing.Type)
                {
                    case LeadershipType.Kurin or LeadershipType.KV:
                        existing.KurinKey = request.EntityKey;
                        existing.GroupKey = null;
                        break;
                    case LeadershipType.Group:
                        existing.GroupKey = request.EntityKey;
                        existing.KurinKey = null;
                        break;
                }
                existing.LeadershipHistories.Clear();
                ApplyLeadershipHistoryChanges(existing, request.LeadershipHistoryMembers, today);
                _unitOfWork.Leaderships.Add(existing, cancellationToken);
                isCreated = true;
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<LeadershipResponse>(ResultType.InternalServerError);
            }

            var response = _mapper.Map<LeadershipResponse>(existing);

            return isCreated
                ? new ServiceResult<LeadershipResponse>(ResultType.Created, response, CreatedAtActionName: "GetLeadershipByKey", CreatedAtRouteValues: new { leadershipKey = response.LeadershipKey })
                : new ServiceResult<LeadershipResponse>(ResultType.Success, response);
        }

        private static void ApplyLeadershipHistoryChanges(
            LeadershipEntity leadership,
            IEnumerable<LeadershipHistoryMemberDto> requestedHistories,
            DateOnly today)
        {
            var requestedAssignments = requestedHistories
                .Where(history => history.EndDate == null)
                .Where(history => history.Member?.MemberKey is Guid memberKey && memberKey != Guid.Empty)
                .Where(history => !string.IsNullOrWhiteSpace(history.Role))
                .Select(history => new
                {
                    Role = Enum.Parse<LeadershipRole>(history.Role, ignoreCase: true),
                    MemberKey = history.Member.MemberKey
                })
                .Distinct()
                .ToList();

            var activeHistories = leadership.LeadershipHistories
                .Where(history => history.EndDate == null)
                .ToList();

            foreach (var activeHistory in activeHistories)
            {
                var isStillAssigned = requestedAssignments.Any(assignment =>
                    assignment.Role == activeHistory.Role &&
                    assignment.MemberKey == activeHistory.MemberKey);

                if (!isStillAssigned)
                {
                    activeHistory.EndDate = today;
                }
            }

            foreach (var assignment in requestedAssignments)
            {
                var alreadyActive = activeHistories.Any(history =>
                    history.Role == assignment.Role &&
                    history.MemberKey == assignment.MemberKey);

                if (alreadyActive)
                {
                    continue;
                }

                leadership.LeadershipHistories.Add(new Common.Entities.KurinModule.LeadershipHistory
                {
                    Leadership = leadership,
                    LeadershipKey = leadership.LeadershipKey,
                    Role = assignment.Role,
                    MemberKey = assignment.MemberKey,
                    StartDate = today,
                    EndDate = null
                });
            }
        }
    }
}
