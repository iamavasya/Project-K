using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers
{
    public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, ServiceResult<bool>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly ILogger<ChangeUserRoleCommandHandler> _logger;
        private readonly ProjectK.Common.Interfaces.IUnitOfWork _unitOfWork;

        public ChangeUserRoleCommandHandler(
            UserManager<AppUser> userManager,
            ICurrentUserContext currentUserContext,
            ILogger<ChangeUserRoleCommandHandler> logger,
            ProjectK.Common.Interfaces.IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _currentUserContext = currentUserContext;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<bool>> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
        {
            // Get target user
            var targetUser = await _userManager.FindByIdAsync(request.TargetUserId.ToString());
            if (targetUser == null)
            {
                return new ServiceResult<bool>(ResultType.NotFound, false, "Target user not found.");
            }

            // Current user context
            var isAdmin = _currentUserContext.IsInRole(UserRole.Admin.ToString());
            var isManager = _currentUserContext.IsInRole(UserRole.Manager.ToString());
            var currentUserKurinKey = _currentUserContext.KurinKey;

            // Policy check
            if (!isAdmin)
            {
                if (!isManager)
                {
                    return new ServiceResult<bool>(ResultType.Forbidden, false, "You do not have permission to change roles.");
                }

                // Manager restrictions
                if (request.NewRole == UserRole.Admin)
                {
                    return new ServiceResult<bool>(ResultType.Forbidden, false, "Managers cannot promote to Admin.");
                }

                if (targetUser.KurinKey != currentUserKurinKey)
                {
                    return new ServiceResult<bool>(ResultType.Forbidden, false, "You can only manage roles for users in your own Kurin.");
                }
            }

            // Perform Role Change
            var currentRoles = await _userManager.GetRolesAsync(targetUser);

            // If already in role, do nothing
            if (currentRoles.Count == 1 && currentRoles.First() == request.NewRole.ToString())
            {
                return new ServiceResult<bool>(ResultType.Success, true, "User is already in the requested role.");
            }

            // Check if this is a downgrade
            bool isDowngradeToUser = (currentRoles.Contains(UserRole.Mentor.ToString()) || currentRoles.Contains(UserRole.Manager.ToString()))
                                     && request.NewRole == UserRole.User;
            bool isDowngradeToMentor = currentRoles.Contains(UserRole.Manager.ToString()) && request.NewRole == UserRole.Mentor;

            // Remove existing roles
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(targetUser, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return new ServiceResult<bool>(ResultType.BadRequest, false, "Failed to remove existing roles.");
                }
            }

            // Add new role
            var addResult = await _userManager.AddToRoleAsync(targetUser, request.NewRole.ToString());
            if (!addResult.Succeeded)
            {
                // Attempt rollback?
                return new ServiceResult<bool>(ResultType.BadRequest, false, "Failed to assign new role.");
            }

            // Log Side Effects (Audit / Cleanup)
            _logger.LogInformation("AUDIT: User {UserId} changed role of user {TargetUserId} to {NewRole}.",
                _currentUserContext.UserId, targetUser.Id, request.NewRole);

            if (isDowngradeToUser || isDowngradeToMentor)
            {
                _logger.LogInformation("SIDE EFFECT: User {TargetUserId} downgraded to {NewRole}. Revoking explicit group assignments.",
                    targetUser.Id, request.NewRole);

                var assignments = await _unitOfWork.MentorAssignments.GetByMentorUserKeyAsync(targetUser.Id, cancellationToken);
                foreach (var assignment in assignments.Where(a => a.RevokedAtUtc == null))
                {
                    assignment.RevokedAtUtc = DateTime.UtcNow;
                    _unitOfWork.MentorAssignments.Update(assignment, cancellationToken);
                }
            }

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
