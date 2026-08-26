using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers
{
    public class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, ServiceResult<object>>
    {
        private readonly UserManager<AppUser> _userManager;
        public LogoutUserCommandHandler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ServiceResult<object>> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserKey))
            {
                return ServiceResult<object>.Failure(
                    ResultType.Unauthorized,
                    "InvalidToken",
                    "Access token is missing or invalid.");
            }
            var user = await _userManager.FindByIdAsync(request.UserKey);
            if (user == null)
            {
                return ServiceResult<object>.Failure(
                    ResultType.NotFound,
                    "UserNotFound",
                    "User not found.");
            }
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            // Otherwise the next sign-in lands the admin inside the kurin they last
            // stepped into, and /panel bounces them straight back out of it.
            user.ActiveKurinKey = null;
            await _userManager.UpdateAsync(user);
            return new(
                ResultType.Success,
                "User logged out successfully."
            );
        }
    }
}
