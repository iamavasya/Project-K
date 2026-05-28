using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, ServiceResult<LoginUserResponse>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILoginResponseFactory _loginResponseFactory;
        private readonly IActivityLogger _activityLogger;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public LoginUserCommandHandler(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILoginResponseFactory loginResponseFactory,
            IActivityLogger activityLogger,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _loginResponseFactory = loginResponseFactory;
            _activityLogger = activityLogger;
            _configuration = configuration;
        }

        public async Task<ServiceResult<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _activityLogger.TrackFailedLogin(request.Email);
                return new ServiceResult<LoginUserResponse>(ResultType.Unauthorized);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (!result.Succeeded)
            {
                _activityLogger.TrackFailedLogin(request.Email);
                return new ServiceResult<LoginUserResponse>(ResultType.Unauthorized);
            }

            var bypassMfa = _configuration.GetValue<bool>("E2E:BypassPrivilegedMfa");

            if (user.TwoFactorEnabled && !bypassMfa)
            {
                return new ServiceResult<LoginUserResponse>(
                    ResultType.Success,
                    new LoginUserResponse
                    {
                        UserKey = user.Id,
                        Email = user.Email!,
                        RequiresMfa = true
                    });
            }

            var response = await _loginResponseFactory.CreateAsync(user, cancellationToken);

            return new ServiceResult<LoginUserResponse>(
                ResultType.Success,
                response);
        }
    }
}
