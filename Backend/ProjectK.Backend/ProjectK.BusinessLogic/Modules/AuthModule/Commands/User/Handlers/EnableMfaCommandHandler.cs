using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers
{
    public class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, ServiceResult<MfaEnableResponseDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EnableMfaCommandHandler> _logger;

        public EnableMfaCommandHandler(UserManager<AppUser> userManager, ILogger<EnableMfaCommandHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ServiceResult<MfaEnableResponseDto>> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user == null)
            {
                return ServiceResult<MfaEnableResponseDto>.Failure(ResultType.Unauthorized, "Unauthorized", "User not found or unauthorized.");
            }

            var verificationCode = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!isTokenValid)
            {
                return ServiceResult<MfaEnableResponseDto>.Failure(ResultType.BadRequest, "InvalidCode", "Invalid verification code.");
            }

            var enableResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!enableResult.Succeeded)
            {
                return ServiceResult<MfaEnableResponseDto>.Failure(ResultType.BadRequest, "MfaSetupFailed", "Failed to enable MFA.");
            }

            RefreshTokenInvalidation.RevokeRefreshToken(user);
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ServiceResult<MfaEnableResponseDto>.Failure(ResultType.BadRequest, "MfaSetupFailed", "Failed to enable MFA.");
            }

            _logger.LogInformation("Audit: User {UserId} successfully enabled MFA.", user.Id);

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return new ServiceResult<MfaEnableResponseDto>(
                ResultType.Success,
                new MfaEnableResponseDto(true, recoveryCodes ?? Enumerable.Empty<string>()));
        }
    }
}
