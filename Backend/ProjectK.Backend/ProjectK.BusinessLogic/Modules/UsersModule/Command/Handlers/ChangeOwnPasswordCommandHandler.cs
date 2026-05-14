using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command.Handlers
{
    public class ChangeOwnPasswordCommandHandler : IRequestHandler<ChangeOwnPasswordCommand, ServiceResult<bool>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ChangeOwnPasswordCommandHandler> _logger;

        public ChangeOwnPasswordCommandHandler(UserManager<AppUser> userManager, ILogger<ChangeOwnPasswordCommandHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ServiceResult<bool>> Handle(ChangeOwnPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserKey.ToString());
            if (user == null)
            {
                return ServiceResult<bool>.Failure(ResultType.Unauthorized, "Unauthorized", "User not found or unauthorized.");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return ServiceResult<bool>.Failure(ResultType.BadRequest, "PasswordChangeFailed", "Failed to change password. Please check your current password and try again.");
            }

            RefreshTokenInvalidation.RevokeRefreshToken(user);
            var updateResult = await _userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                _logger.LogInformation("Audit: User {UserId} successfully changed their password.", user.Id);
                return new ServiceResult<bool>(ResultType.Success, true);
            }

            return ServiceResult<bool>.Failure(ResultType.BadRequest, "PasswordChangeFailed", "Failed to change password. Please try again.");
        }
    }
}
