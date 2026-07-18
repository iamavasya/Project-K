using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Enums;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Setup.Handlers
{
    public class InitializeSetupCommandHandler : IRequestHandler<InitializeSetupCommand, ServiceResult<LoginUserResponse>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILoginResponseFactory _loginResponseFactory;

        public InitializeSetupCommandHandler(UserManager<AppUser> userManager, ILoginResponseFactory loginResponseFactory)
        {
            _userManager = userManager;
            _loginResponseFactory = loginResponseFactory;
        }

        public async Task<ServiceResult<LoginUserResponse>> Handle(InitializeSetupCommand request, CancellationToken cancellationToken)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(UserRole.Admin.ToString());
            if (adminUsers.Any())
            {
                return new ServiceResult<LoginUserResponse>(ResultType.Forbidden, null, "System is already initialized.");
            }

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                FirstName = request.FirstName,
                LastName = request.LastName,
                OnboardingStatus = OnboardingStatus.Active
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return new ServiceResult<LoginUserResponse>(ResultType.BadRequest, null, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, UserRole.Admin.ToString());

            var response = await _loginResponseFactory.CreateAsync(user, cancellationToken);
            return new ServiceResult<LoginUserResponse>(ResultType.Success, response);
        }
    }
}
