using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.BusinessLogic.Modules.UsersModule.Queries;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Enums;
using System.Security.Claims;

namespace ProjectK.API.Controllers.UsersModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        public readonly IMediator _mediator;
        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var request = new GetAllUsersQuery();
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpGet("me")]
        public async Task<IActionResult> GetAccountSettings()
        {
            var userKey = GetCurrentUserKey();
            var response = await _mediator.Send(new GetAccountSettingsQuery(userKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateAccountProfile([FromBody] UpdateAccountProfileRequestDto request)
        {
            var userKey = GetCurrentUserKey();
            var response = await _mediator.Send(new UpdateAccountProfileCommand(userKey, request.Email, request.PhoneNumber, request.CurrentPassword));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("me/email/confirm")]
        public async Task<IActionResult> ConfirmAccountEmailChange([FromBody] ConfirmAccountEmailChangeRequestDto request)
        {
            var userKey = GetCurrentUserKey();
            var response = await _mediator.Send(new ConfirmAccountEmailChangeCommand(userKey, request.Email, request.Token));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            var userKey = GetCurrentUserKey();
            var response = await _mediator.Send(new ChangeOwnPasswordCommand(userKey, request.CurrentPassword, request.NewPassword));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("me/mfa/reset")]
        public async Task<IActionResult> ResetMfa([FromBody] ResetMfaRequestDto request)
        {
            var userKey = GetCurrentUserKey();
            var response = await _mediator.Send(new ResetOwnMfaCommand(userKey, request.CurrentPassword));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("me/mfa/disable")]
        public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequestDto request)
        {
            var userKey = GetCurrentUserKey();
            var response = await _mediator.Send(new DisableOwnMfaCommand(userKey, request.CurrentPassword));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("{userId}/mfa/reset")]
        public async Task<IActionResult> ResetUserMfa(Guid userId)
        {
            var response = await _mediator.Send(new ResetUserMfaCommand(userId));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var request = new DeleteUserCommand(userId);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPost("{userId}/role")]
        public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] UserRole newRole)
        {
            var request = new ChangeUserRoleCommand(userId, newRole);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        private Guid GetCurrentUserKey()
        {
            var userKeyClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userKeyClaim!);
        }
    }
}
