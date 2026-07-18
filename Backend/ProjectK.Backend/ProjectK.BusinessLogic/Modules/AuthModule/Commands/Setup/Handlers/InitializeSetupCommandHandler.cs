using MediatR;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Enums;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Setup.Handlers
{
    public class InitializeSetupCommandHandler : IRequestHandler<InitializeSetupCommand, ServiceResult<LoginUserResponse>>
    {
        // Guards against two concurrent initialize calls both passing the "no admins yet" check.
        private static readonly SemaphoreSlim InitializationLock = new(1, 1);

        private readonly UserManager<AppUser> _userManager;
        private readonly ILoginResponseFactory _loginResponseFactory;
        private readonly ISystemSettingsService _systemSettings;
        private readonly IDemoDataSeeder _demoDataSeeder;

        public InitializeSetupCommandHandler(
            UserManager<AppUser> userManager,
            ILoginResponseFactory loginResponseFactory,
            ISystemSettingsService systemSettings,
            IDemoDataSeeder demoDataSeeder)
        {
            _userManager = userManager;
            _loginResponseFactory = loginResponseFactory;
            _systemSettings = systemSettings;
            _demoDataSeeder = demoDataSeeder;
        }

        public async Task<ServiceResult<LoginUserResponse>> Handle(InitializeSetupCommand request, CancellationToken cancellationToken)
        {
            await InitializationLock.WaitAsync(cancellationToken);
            try
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

                await _systemSettings.SetValueAsync(
                    SystemSettingKeys.EnforcePrivilegedMfa,
                    request.EnforcePrivilegedMfa ? "true" : "false",
                    cancellationToken);

                if (request.SeedDemoData)
                {
                    await _demoDataSeeder.SeedAsync(cancellationToken);
                }

                var response = await _loginResponseFactory.CreateAsync(user, cancellationToken);
                return new ServiceResult<LoginUserResponse>(ResultType.Success, response);
            }
            finally
            {
                InitializationLock.Release();
            }
        }
    }
}
