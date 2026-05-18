using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers
{
    public class VerifyMfaLoginCommandHandler : IRequestHandler<VerifyMfaLoginCommand, ServiceResult<LoginUserResponse>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILoginResponseFactory _loginResponseFactory;
        private readonly ILogger<VerifyMfaLoginCommandHandler> _logger;
        private readonly IActivityLogger _activityLogger;

        public VerifyMfaLoginCommandHandler(
            UserManager<AppUser> userManager,
            ILoginResponseFactory loginResponseFactory,
            ILogger<VerifyMfaLoginCommandHandler> logger,
            IActivityLogger activityLogger)
        {
            _userManager = userManager;
            _loginResponseFactory = loginResponseFactory;
            _logger = logger;
            _activityLogger = activityLogger;
        }

        public async Task<ServiceResult<LoginUserResponse>> Handle(VerifyMfaLoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _activityLogger.TrackFailedMfa(request.Email);
                return ServiceResult<LoginUserResponse>.Failure(ResultType.Unauthorized, "InvalidCredentials", "Invalid verification code or recovery code.");
            }

            var verificationCode = request.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                verificationCode);

            if (!isTokenValid)
            {
                var recoveryCodeResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.Code.Trim());
                if (!recoveryCodeResult.Succeeded)
                {
                    _activityLogger.TrackFailedMfa(request.Email);
                    return ServiceResult<LoginUserResponse>.Failure(ResultType.Unauthorized, "InvalidCredentials", "Invalid verification code or recovery code.");
                }

                _logger.LogInformation("Audit: User {UserId} successfully logged in using a recovery code.", user.Id);
            }

            var response = await _loginResponseFactory.CreateAsync(user, cancellationToken);

            return new ServiceResult<LoginUserResponse>(ResultType.Success, response);
        }
    }
}
