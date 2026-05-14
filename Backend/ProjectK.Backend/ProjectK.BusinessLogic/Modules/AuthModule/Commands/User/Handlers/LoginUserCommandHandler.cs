using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User.Handlers
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, ServiceResult<LoginUserResponse>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILoginResponseFactory _loginResponseFactory;

        public LoginUserCommandHandler(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILoginResponseFactory loginResponseFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _loginResponseFactory = loginResponseFactory;
        }

        public async Task<ServiceResult<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new ServiceResult<LoginUserResponse>(ResultType.Unauthorized);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (!result.Succeeded)
            {
                return new ServiceResult<LoginUserResponse>(ResultType.Unauthorized);
            }

            if (user.TwoFactorEnabled)
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
