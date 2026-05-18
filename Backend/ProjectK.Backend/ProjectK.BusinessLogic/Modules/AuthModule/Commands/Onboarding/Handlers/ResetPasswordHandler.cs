using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding.Handlers
{
    public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ServiceResult<bool>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IActivityLogger _activityLogger;

        public ResetPasswordHandler(
            UserManager<AppUser> userManager,
            IActivityLogger activityLogger)
        {
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        public async Task<ServiceResult<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || user.OnboardingStatus != OnboardingStatus.Active)
            {
                return ServiceResult<bool>.Failure(ResultType.BadRequest, "InvalidResetAttempt", "Invalid reset attempt. Please check your email or token.");
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return ServiceResult<bool>.Failure(ResultType.BadRequest, "InvalidResetAttempt", "Invalid reset attempt. Please check your email or token.");
            }

            _activityLogger.LogAudit(
                action: "Account.PasswordResetCompleted",
                actorUserId: user.Id,
                targetUserId: user.Id,
                email: user.Email,
                reason: "Password reset completed with reset token.");

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
