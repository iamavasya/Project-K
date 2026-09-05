using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.BusinessLogic.Services.Caching;
using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectK.Common.Interfaces.Modules.AuthModule;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment.Assign
{
    public class AssignMentorCommandHandler : IRequestHandler<AssignMentorCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILeadershipRoleSyncService _roleSync;
        private readonly IBackendCache _cache;

        public AssignMentorCommandHandler(IUnitOfWork unitOfWork, ILeadershipRoleSyncService roleSync, IBackendCache cache)
        {
            _unitOfWork = unitOfWork;
            _roleSync = roleSync;
            _cache = cache;
        }

        public async Task<ServiceResult<Guid>> Handle(AssignMentorCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey, cancellationToken);
            if (group == null)
            {
                return ServiceResult<Guid>.Failure(ResultType.NotFound, "GroupNotFound", "Group not found.");
            }

            var mentorMember = await _unitOfWork.Members.GetByUserKeyAsync(request.MentorUserKey, cancellationToken);
            if (mentorMember == null)
            {
                return ServiceResult<Guid>.Failure(ResultType.NotFound, "MentorNotFound", "Mentor member profile not found.");
            }

            if (mentorMember.KurinKey != group.KurinKey)
            {
                return ServiceResult<Guid>.Failure(ResultType.Forbidden, "MentorFromAnotherKurin", "Mentor must belong to the same Kurin as the Group.");
            }

            var existingAssignment = await _unitOfWork.MentorAssignments.GetSpecificAssignmentAsync(request.MentorUserKey, request.GroupKey, cancellationToken);
            if (existingAssignment != null && existingAssignment.RevokedAtUtc == null)
            {
                // The existing key used to ride along as Data, but it landed in CreatedAtActionName and the
                // client never read it; the message is what the caller needs.
                return ServiceResult<Guid>.Failure(
                    ResultType.Conflict,
                    "MentorAlreadyAssigned",
                    "Mentor is already assigned to this group.");
            }

            var assignment = new ProjectK.Common.Entities.KurinModule.MentorAssignment
            {
                MentorAssignmentKey = Guid.NewGuid(),
                MentorUserKey = request.MentorUserKey,
                GroupKey = request.GroupKey,
                AssignedAtUtc = DateTime.UtcNow
            };

            _unitOfWork.MentorAssignments.Create(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // The assignment grants гуртковий access; realign the member's roles and drop the cached
            // scope set so the next authorization check reflects it immediately.
            await _roleSync.SyncMemberAsync(mentorMember.MemberKey, cancellationToken);
            _cache.Invalidate(BackendCachePolicies.MentorScopeReads);

            return new ServiceResult<Guid>(ResultType.Success, assignment.MentorAssignmentKey);
        }
    }
}
