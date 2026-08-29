using MediatR;
using ProjectK.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectK.BusinessLogic.Modules.UsersModule.Models;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.Common.Models.Enums;
using System.Collections.Generic;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.ChangePassword;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.ConfirmEmailChange;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.DisableMfa;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.Get;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.ResetMfa;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.UpdateProfile;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.TileLayout.Get;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.TileLayout.Reset;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.TileLayout.Save;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.User.ChangeRole;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.User.Delete;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.User.Get;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.User.ResetMfa;
using ProjectK.Common.Models.Dtos.UsersModule;

namespace ProjectK.API.Controllers.UsersModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpGet("users")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUsers()
        {
            var request = new GetAllUsersQuery();
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpGet("me")]
        [ProducesResponseType(typeof(AccountSettingsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAccountSettings()
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var response = await _mediator.Send(new GetAccountSettingsQuery(userKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPut("me")]
        [ProducesResponseType(typeof(AccountSettingsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateAccountProfile([FromBody] UpdateAccountProfileRequestDto request)
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var response = await _mediator.Send(new UpdateAccountProfileCommand(userKey, request.Email, request.PhoneNumber, request.CurrentPassword));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("me/email/confirm")]
        [ProducesResponseType(typeof(AccountSettingsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ConfirmAccountEmailChange([FromBody] ConfirmAccountEmailChangeRequestDto request)
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var response = await _mediator.Send(new ConfirmAccountEmailChangeCommand(userKey, request.Email, request.Token));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("me/password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var response = await _mediator.Send(new ChangeOwnPasswordCommand(userKey, request.CurrentPassword, request.NewPassword));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("me/mfa/reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ResetMfa([FromBody] ResetMfaRequestDto request)
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var response = await _mediator.Send(new ResetOwnMfaCommand(userKey, request.CurrentPassword));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("me/mfa/disable")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequestDto request)
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var response = await _mediator.Send(new DisableOwnMfaCommand(userKey, request.CurrentPassword));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpGet("me/layouts")]
        [ProducesResponseType(typeof(IReadOnlyList<TileLayoutDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTileLayouts()
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var response = await _mediator.Send(new GetTileLayoutsQuery(userKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPut("me/layouts/{boardKey}")]
        [ProducesResponseType(typeof(TileLayoutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SaveTileLayout(string boardKey, [FromBody] SaveTileLayoutRequestDto request)
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var tileKeys = request.TileKeys ?? new List<string>();
            var response = await _mediator.Send(new SaveTileLayoutCommand(userKey, boardKey, tileKeys, request.SchemaVersion));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpDelete("me/layouts/{boardKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ResetTileLayout(string boardKey)
        {
            if (!TryGetCurrentUserKey(out var userKey))
            {
                return this.UnreadableIdentity();
            }

            var response = await _mediator.Send(new ResetTileLayoutCommand(userKey, boardKey));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("{userId}/mfa/reset")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetUserMfa(Guid userId)
        {
            var response = await _mediator.Send(new ResetUserMfaCommand(userId));
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpDelete("{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var request = new DeleteUserCommand(userId);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPost("{userId}/role")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] UserRole newRole)
        {
            if (!Enum.IsDefined(newRole))
            {
                return this.Failure(ResultType.BadRequest, "InvalidRole", "Unknown user role.");
            }

            var request = new ChangeUserRoleCommand(userId, newRole);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }

        private bool TryGetCurrentUserKey(out Guid userKey)
        {
            userKey = this.UserKey() ?? Guid.Empty;
            return userKey != Guid.Empty;
        }
    }
}
