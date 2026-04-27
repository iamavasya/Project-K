using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment
{
    public class AssignMentorCommandHandler : IRequestHandler<AssignMentorCommand, ServiceResult<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public AssignMentorCommandHandler(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<ServiceResult<Guid>> Handle(AssignMentorCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.Groups.GetByKeyAsync(request.GroupKey, cancellationToken);
            if (group == null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound, Guid.Empty, "Group not found.");
            }

            var mentorMember = await _unitOfWork.Members.GetByUserKeyAsync(request.MentorUserKey, cancellationToken);
            if (mentorMember == null)
            {
                return new ServiceResult<Guid>(ResultType.NotFound, Guid.Empty, "Mentor member profile not found.");
            }

            if (mentorMember.KurinKey != group.KurinKey)
            {
                return new ServiceResult<Guid>(ResultType.Forbidden, Guid.Empty, "Mentor must belong to the same Kurin as the Group.");
            }

            var existingAssignment = await _unitOfWork.MentorAssignments.GetSpecificAssignmentAsync(request.MentorUserKey, request.GroupKey, cancellationToken);
            if (existingAssignment != null && existingAssignment.RevokedAtUtc == null)
            {
                return new ServiceResult<Guid>(ResultType.Conflict, existingAssignment.MentorAssignmentKey, "Mentor is already assigned to this group.");
            }

            var assignment = new ProjectK.Common.Entities.KurinModule.MentorAssignment
            {
                MentorAssignmentKey = Guid.NewGuid(),
                MentorUserKey = request.MentorUserKey,
                GroupKey = request.GroupKey,
                AssignedAtUtc = DateTime.UtcNow
            };

            _unitOfWork.MentorAssignments.Create(assignment, cancellationToken);
            await EnsureMentorRoleAsync(request.MentorUserKey);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ServiceResult<Guid>(ResultType.Success, assignment.MentorAssignmentKey);
        }

        private async Task EnsureMentorRoleAsync(Guid mentorUserKey)
        {
            var user = await _userManager.FindByIdAsync(mentorUserKey.ToString());
            if (user == null)
            {
                return;
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(UserRole.Admin.ToClaimValue()) || currentRoles.Contains(UserRole.Manager.ToClaimValue()))
            {
                return;
            }

            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await _userManager.AddToRoleAsync(user, UserRole.Mentor.ToClaimValue());
        }
    }
}
