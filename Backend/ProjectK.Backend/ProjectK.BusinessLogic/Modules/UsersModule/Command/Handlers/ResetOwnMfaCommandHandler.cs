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
    public class ResetOwnMfaCommandHandler : IRequestHandler<ResetOwnMfaCommand, ServiceResult<bool>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ResetOwnMfaCommandHandler> _logger;
        private readonly IActivityLogger _activityLogger;

        public ResetOwnMfaCommandHandler(
            UserManager<AppUser> userManager,
            ILogger<ResetOwnMfaCommandHandler> logger,
            IActivityLogger activityLogger)
        {
            _userManager = userManager;
            _logger = logger;
            _activityLogger = activityLogger;
        }

        public async Task<ServiceResult<bool>> Handle(ResetOwnMfaCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user == null)
            {
                return ServiceResult<bool>.Failure(ResultType.Unauthorized, "Unauthorized", "User not found or unauthorized.");
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return ServiceResult<bool>.Failure(ResultType.Unauthorized, "InvalidCredentials", "Invalid credentials.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!passwordValid)
            {
                return ServiceResult<bool>.Failure(ResultType.Unauthorized, "InvalidCredentials", "Invalid credentials.");
            }

            var disableResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disableResult.Succeeded)
            {
                return ServiceResult<bool>.Failure(ResultType.BadRequest, "MfaResetFailed", "Failed to reset MFA.");
            }

            var resetResult = await _userManager.ResetAuthenticatorKeyAsync(user);
            if (!resetResult.Succeeded)
            {
                return ServiceResult<bool>.Failure(ResultType.BadRequest, "MfaResetFailed", "Failed to reset MFA.");
            }

            RefreshTokenInvalidation.RevokeRefreshToken(user);
            var updateResult = await _userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                _activityLogger.LogAudit(
                    action: "Account.MfaReset",
                    actorUserId: user.Id,
                    targetUserId: user.Id,
                    reason: "User reset their own MFA.");
                return new ServiceResult<bool>(ResultType.Success, true);
            }

            return ServiceResult<bool>.Failure(ResultType.BadRequest, "MfaResetFailed", "Failed to reset MFA.");
        }
    }
}
