using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Models;

namespace ProjectK.API.Services
{
    public interface IMfaEnforcementPolicy
    {
        Task<bool> IsPrivilegedMfaRequiredAsync(CancellationToken cancellationToken = default);
    }

    public class MfaEnforcementPolicy : IMfaEnforcementPolicy
    {
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ISystemSettingsService _systemSettings;

        public MfaEnforcementPolicy(
            IHostEnvironment environment,
            IConfiguration configuration,
            ISystemSettingsService systemSettings)
        {
            _environment = environment;
            _configuration = configuration;
            _systemSettings = systemSettings;
        }

        public async Task<bool> IsPrivilegedMfaRequiredAsync(CancellationToken cancellationToken = default)
        {
            if (_environment.IsDevelopment())
            {
                return false;
            }

            // The env-gated bypass is for LAN/test tiers only. Production and Staging ignore it outright,
            // so a stray E2E__BypassPrivilegedMfa in a deployed env file can never disable privileged MFA.
            if (!_environment.IsProduction()
                && !_environment.IsStaging()
                && _configuration.GetValue<bool>("E2E:BypassPrivilegedMfa", false))
            {
                return false;
            }

            if (_environment.EnvironmentName == "SelfHost")
            {
                return await _systemSettings.GetBoolAsync(
                    SystemSettingKeys.EnforcePrivilegedMfa,
                    defaultValue: false,
                    cancellationToken);
            }

            return true;
        }
    }
}
