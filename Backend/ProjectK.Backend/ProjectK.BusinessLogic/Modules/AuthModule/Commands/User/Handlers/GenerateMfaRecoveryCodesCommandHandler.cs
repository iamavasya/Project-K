using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers
{
    public class GenerateMfaRecoveryCodesCommandHandler
        : IRequestHandler<GenerateMfaRecoveryCodesCommand, ServiceResult<MfaRecoveryCodesResponseDto>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<GenerateMfaRecoveryCodesCommandHandler> _logger;

        public GenerateMfaRecoveryCodesCommandHandler(UserManager<AppUser> userManager, ILogger<GenerateMfaRecoveryCodesCommandHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ServiceResult<MfaRecoveryCodesResponseDto>> Handle(
            GenerateMfaRecoveryCodesCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user == null)
            {
                return ServiceResult<MfaRecoveryCodesResponseDto>.Failure(ResultType.Unauthorized, "Unauthorized", "User not found or unauthorized.");
            }

            if (!user.TwoFactorEnabled)
            {
                return ServiceResult<MfaRecoveryCodesResponseDto>.Failure(ResultType.BadRequest, "MfaNotEnabled", "MFA is not enabled.");
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword)
                || !await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                return ServiceResult<MfaRecoveryCodesResponseDto>.Failure(ResultType.Unauthorized, "InvalidCredentials", "Invalid credentials.");
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            _logger.LogInformation("Audit: User {UserId} successfully rotated/generated their MFA recovery codes.", user.Id);

            return new ServiceResult<MfaRecoveryCodesResponseDto>(
                ResultType.Success,
                new MfaRecoveryCodesResponseDto(recoveryCodes ?? Enumerable.Empty<string>()));
        }
    }
}
