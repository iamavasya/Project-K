using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers
{
    public class ResetUserMfaCommandHandler : IRequestHandler<ResetUserMfaCommand, ServiceResult<bool>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly ILogger<ResetUserMfaCommandHandler> _logger;
        private readonly IActivityLogger _activityLogger;

        public ResetUserMfaCommandHandler(
            UserManager<AppUser> userManager,
            ICurrentUserContext currentUserContext,
            ILogger<ResetUserMfaCommandHandler> logger,
            IActivityLogger activityLogger)
        {
            _userManager = userManager;
            _currentUserContext = currentUserContext;
            _logger = logger;
            _activityLogger = activityLogger;
        }

        public async Task<ServiceResult<bool>> Handle(ResetUserMfaCommand request, CancellationToken cancellationToken)
        {
            var targetUser = await _userManager.FindByIdAsync(request.TargetUserKey.ToString());
            if (targetUser == null)
            {
                return ServiceResult<bool>.Failure(ResultType.NotFound, "UserNotFound", "Target user not found.");
            }

            var isAdmin = _currentUserContext.IsInRole(UserRole.Admin.ToString());
            var isManager = _currentUserContext.IsInRole(UserRole.Manager.ToString());
            if (!isAdmin && !isManager)
            {
                return ServiceResult<bool>.Failure(ResultType.Forbidden, "Forbidden", "You do not have permission to perform this action.");
            }

            var targetRoles = await _userManager.GetRolesAsync(targetUser);
            if (isManager)
            {
                if (targetUser.KurinKey != _currentUserContext.KurinKey)
                {
                    return ServiceResult<bool>.Failure(ResultType.Forbidden, "Forbidden", "Managers can reset MFA only in their own Kurin.");
                }

                if (targetRoles.Contains(UserRole.Admin.ToString()) || targetRoles.Contains(UserRole.Manager.ToString()))
                {
                    return ServiceResult<bool>.Failure(ResultType.Forbidden, "CannotResetPrivilegedMfa", "Managers cannot reset MFA for privileged users.");
                }
            }

            if (targetUser.TwoFactorEnabled)
            {
                var disableResult = await _userManager.SetTwoFactorEnabledAsync(targetUser, false);
                if (!disableResult.Succeeded)
                {
                    return ServiceResult<bool>.Failure(ResultType.BadRequest, "MfaResetFailed", "Failed to reset MFA.");
                }
            }

            var resetResult = await _userManager.ResetAuthenticatorKeyAsync(targetUser);
            if (!resetResult.Succeeded)
            {
                return ServiceResult<bool>.Failure(ResultType.BadRequest, "MfaResetFailed", "Failed to reset MFA.");
            }

            RefreshTokenInvalidation.RevokeRefreshToken(targetUser);
            var updateResult = await _userManager.UpdateAsync(targetUser);

            if (updateResult.Succeeded)
            {
                _activityLogger.LogAudit(
                    action: "Admin.UserMfaReset",
                    actorUserId: _currentUserContext.UserId,
                    targetUserId: targetUser.Id,
                    reason: "Privileged user reset MFA for another account.");
                return new ServiceResult<bool>(ResultType.Success, true);
            }

            return ServiceResult<bool>.Failure(ResultType.BadRequest, "MfaResetFailed", "Failed to reset MFA.");
        }
    }
}
