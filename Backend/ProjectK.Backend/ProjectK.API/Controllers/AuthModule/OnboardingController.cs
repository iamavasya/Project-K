using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ActivateAccount;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ApproveWaitlistEntry;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.GetStats;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.GetWaitlistEntries;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.RejectWaitlistEntry;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.RequestPasswordReset;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ResendInvitation;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ResetPassword;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.SubmitWaitlistRegistration;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ValidateInvitationToken;
using ProjectK.API.Authorization;

namespace ProjectK.API.Controllers.AuthModule
{
    [Route("api/auth/onboarding")]
    [ApiController]
    public class OnboardingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OnboardingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [HttpPost("waitlist")]
        public async Task<IActionResult> SubmitWaitlistRegistration([FromBody] SubmitWaitlistRegistrationCommand command)
        {
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpPost("waitlist/{key}/approve")]
        public async Task<IActionResult> ApproveWaitlistEntry(Guid key)
        {
            var response = await _mediator.Send(new ApproveWaitlistEntryCommand(key));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpPost("waitlist/{key}/resend-invitation")]
        public async Task<IActionResult> ResendInvitation(Guid key)
        {
            var response = await _mediator.Send(new ResendInvitationCommand(key));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpPost("waitlist/{key}/reject")]
        public async Task<IActionResult> RejectWaitlistEntry(Guid key, [FromBody] string? note)
        {
            var response = await _mediator.Send(new RejectWaitlistEntryCommand(key, note));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpGet("waitlist")]
        public async Task<IActionResult> GetWaitlistEntries()
        {
            var response = await _mediator.Send(new GetWaitlistEntriesQuery());
            return response.ToActionResult(this);
        }

        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpGet("stats")]
        public async Task<IActionResult> GetOnboardingStats([FromQuery] Guid? kurinKey)
        {
            var response = await _mediator.Send(new GetOnboardingStatsQuery(kurinKey));
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [HttpGet("invitation/{token}/validate")]
        public async Task<IActionResult> ValidateInvitationToken(string token)
        {
            var response = await _mediator.Send(new ValidateInvitationTokenQuery(token));
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [HttpPost("activate")]
        public async Task<IActionResult> ActivateAccount([FromBody] ActivateAccountCommand command)
        {
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("password-reset/request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
        {
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("password-reset/reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
