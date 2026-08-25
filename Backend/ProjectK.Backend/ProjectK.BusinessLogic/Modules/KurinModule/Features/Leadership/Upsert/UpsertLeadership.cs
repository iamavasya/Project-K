using AutoMapper;
using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;
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

        /// <summary>
        /// Set only by server-side flows that seat an office with no signed-in assigner — account
        /// activation makes the new kurin leader the Зв'язковий while the caller is still anonymous.
        /// It is absent from <see cref="UpsertLeadershipRequest"/>, so an HTTP caller cannot set it.
        /// </summary>
        public bool SeatedBySystem { get; init; }

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
        private readonly ILeadershipRoleSyncService _roleSync;
        private readonly ICurrentUserContext _currentUserContext;
        public UpsertLeadershipHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILeadershipRoleSyncService roleSync,
            ICurrentUserContext currentUserContext)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _roleSync = roleSync;
            _currentUserContext = currentUserContext;
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
                if (!request.SeatedBySystem && !MayAssignTouchedOffices(existing, request.LeadershipHistoryMembers, existing.Type))
                {
                    return new ServiceResult<LeadershipResponse>(ResultType.Forbidden);
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
                if (!request.SeatedBySystem && !MayAssignTouchedOffices(existing, request.LeadershipHistoryMembers, existing.Type))
                {
                    return new ServiceResult<LeadershipResponse>(ResultType.Forbidden);
                }

                ApplyLeadershipHistoryChanges(existing, request.LeadershipHistoryMembers, today);
                _unitOfWork.Leaderships.Add(existing, cancellationToken);
                isCreated = true;
            }

            var changes = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (changes <= 0)
            {
                return new ServiceResult<LeadershipResponse>(ResultType.InternalServerError);
            }

            // Realign system roles for everyone whose office assignment was touched (added or ended).
            var affectedMembers = existing.LeadershipHistories
                .Select(history => history.MemberKey)
                .Distinct()
                .ToList();
            await _roleSync.SyncMembersAsync(affectedMembers, cancellationToken);

            var response = _mapper.Map<LeadershipResponse>(existing);

            return isCreated
                ? new ServiceResult<LeadershipResponse>(ResultType.Created, response, CreatedAtActionName: "GetLeadershipByKey", CreatedAtRouteValues: new { leadershipKey = response.LeadershipKey })
                : new ServiceResult<LeadershipResponse>(ResultType.Success, response);
        }

        /// <summary>
        /// Rejects the request when it would seat or unseat an office the caller may not assign. Only
        /// the offices actually changing are checked — resubmitting an untouched провід stays allowed.
        /// </summary>
        private bool MayAssignTouchedOffices(
            LeadershipEntity leadership,
            IEnumerable<LeadershipHistoryMemberDto> requestedHistories,
            LeadershipType type)
        {
            var assignable = RolePermissionMap.AssignableOffices(_currentUserContext.Roles);
            var requested = ParseRequestedAssignments(requestedHistories);
            var active = leadership.LeadershipHistories
                .Where(history => history.EndDate == null)
                .ToList();

            var touched = new HashSet<LeadershipRole>();
            foreach (var activeHistory in active)
            {
                var isStillAssigned = requested.Any(assignment =>
                    assignment.Role == activeHistory.Role && assignment.MemberKey == activeHistory.MemberKey);
                if (!isStillAssigned)
                {
                    touched.Add(activeHistory.Role);
                }
            }

            foreach (var assignment in requested)
            {
                var alreadyActive = active.Any(history =>
                    history.Role == assignment.Role && history.MemberKey == assignment.MemberKey);
                if (!alreadyActive)
                {
                    touched.Add(assignment.Role);
                }
            }

            return touched.All(role => assignable.Contains((type, role)));
        }

        private static List<(LeadershipRole Role, Guid MemberKey)> ParseRequestedAssignments(
            IEnumerable<LeadershipHistoryMemberDto> requestedHistories) =>
            requestedHistories
                .Where(history => history.EndDate == null)
                .Where(history => history.Member?.MemberKey is Guid memberKey && memberKey != Guid.Empty)
                .Where(history => !string.IsNullOrWhiteSpace(history.Role))
                .Select(history => (
                    Role: Enum.Parse<LeadershipRole>(history.Role, ignoreCase: true),
                    MemberKey: history.Member.MemberKey))
                .Distinct()
                .ToList();

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
