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

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment.Revoke
{
    public class RevokeMentorCommandHandler : IRequestHandler<RevokeMentorCommand, ServiceResult<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILeadershipRoleSyncService _roleSync;
        private readonly IBackendCache _cache;

        public RevokeMentorCommandHandler(IUnitOfWork unitOfWork, ILeadershipRoleSyncService roleSync, IBackendCache cache)
        {
            _unitOfWork = unitOfWork;
            _roleSync = roleSync;
            _cache = cache;
        }

        public async Task<ServiceResult<bool>> Handle(RevokeMentorCommand request, CancellationToken cancellationToken)
        {
            var existingAssignment = await _unitOfWork.MentorAssignments.GetSpecificAssignmentAsync(request.MentorUserKey, request.GroupKey, cancellationToken);

            if (existingAssignment == null)
            {
                return ServiceResult<bool>.Failure(ResultType.NotFound, "MentorAssignmentNotFound", "Mentor assignment not found.");
            }

            if (existingAssignment.RevokedAtUtc != null)
            {
                return new ServiceResult<bool>(ResultType.Success, true, "Assignment was already revoked.");
            }

            existingAssignment.RevokedAtUtc = DateTime.UtcNow;
            _unitOfWork.MentorAssignments.Update(existingAssignment, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Revocation must take effect at once, not after the TTL — otherwise the
            // mentor keeps write access to the group until the cached set expires.
            var mentorMember = await _unitOfWork.Members.GetByUserKeyAsync(request.MentorUserKey, cancellationToken);
            if (mentorMember is not null)
            {
                await _roleSync.SyncMemberAsync(mentorMember.MemberKey, cancellationToken);
            }

            _cache.Invalidate(BackendCachePolicies.MentorScopeReads);

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
