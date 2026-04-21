using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
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

        public ResetPasswordHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ServiceResult<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || user.OnboardingStatus != OnboardingStatus.Active)
            {
                return new ServiceResult<bool>(ResultType.NotFound, false, "User not found or not active.");
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ServiceResult<bool>(ResultType.BadRequest, false, $"Failed to reset password: {errors}");
            }

            return new ServiceResult<bool>(ResultType.Success, true);
        }
    }
}
