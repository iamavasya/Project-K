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
    public class DisableOwnMfaCommandHandler : IRequestHandler<DisableOwnMfaCommand, ServiceResult<bool>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DisableOwnMfaCommandHandler> _logger;
        private readonly IActivityLogger _activityLogger;

        public DisableOwnMfaCommandHandler(
            UserManager<AppUser> userManager,
            ILogger<DisableOwnMfaCommandHandler> logger,
            IActivityLogger activityLogger)
        {
            _userManager = userManager;
            _logger = logger;
            _activityLogger = activityLogger;
        }

        public async Task<ServiceResult<bool>> Handle(DisableOwnMfaCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user == null)
            {
                return ServiceResult<bool>.Failure(ResultType.Unauthorized, "Unauthorized", "User not found or unauthorized.");
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword)
                || !await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                return ServiceResult<bool>.Failure(ResultType.Unauthorized, "InvalidCredentials", "Invalid credentials.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(UserRole.Admin.ToString()) || roles.Contains(UserRole.Manager.ToString()))
            {
                return ServiceResult<bool>.Failure(ResultType.Forbidden, "MfaRequired", "Privileged users must keep MFA enabled. Reset MFA to reconfigure it.");
            }

            if (!user.TwoFactorEnabled)
            {
                return new ServiceResult<bool>(ResultType.Success, true);
            }

            var disableResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disableResult.Succeeded)
            {
                return ServiceResult<bool>.Failure(ResultType.BadRequest, "MfaDisableFailed", "Failed to disable MFA.");
            }

            RefreshTokenInvalidation.RevokeRefreshToken(user);
            var updateResult = await _userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                _activityLogger.LogAudit(
                    action: "Account.MfaDisabled",
                    actorUserId: user.Id,
                    targetUserId: user.Id,
                    reason: "User disabled their own MFA.");
                return new ServiceResult<bool>(ResultType.Success, true);
            }

            return ServiceResult<bool>.Failure(ResultType.BadRequest, "MfaDisableFailed", "Failed to disable MFA.");
        }
    }
}
