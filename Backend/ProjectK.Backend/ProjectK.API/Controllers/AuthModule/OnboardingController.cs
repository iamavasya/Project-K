using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries.Onboarding;
using ProjectK.Common.Extensions;
using System.Threading.Tasks;

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

        [Authorize(Policy = "RequireAdmin")]
        [HttpPost("waitlist/{key}/approve")]
        public async Task<IActionResult> ApproveWaitlistEntry(Guid key)
        {
            var response = await _mediator.Send(new ApproveWaitlistEntryCommand(key));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpPost("waitlist/{key}/resend-invitation")]
        public async Task<IActionResult> ResendInvitation(Guid key)
        {
            var response = await _mediator.Send(new ResendInvitationCommand(key));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpPost("waitlist/{key}/reject")]
        public async Task<IActionResult> RejectWaitlistEntry(Guid key, [FromBody] string? note)
        {
            var response = await _mediator.Send(new RejectWaitlistEntryCommand(key, note));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpGet("waitlist")]
        public async Task<IActionResult> GetWaitlistEntries()
        {
            var response = await _mediator.Send(new GetWaitlistEntriesQuery());
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
        [HttpPost("password-reset/request")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
        {
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [HttpPost("password-reset/reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
