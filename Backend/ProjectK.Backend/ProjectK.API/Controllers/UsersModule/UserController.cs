using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.BusinessLogic.Modules.UsersModule.Queries;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;

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

        [Authorize(Policy = "RequireManager")]
        [HttpPost("{userId}/role")]
        public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] UserRole newRole)
        {
            var request = new ChangeUserRoleCommand(userId, newRole);
            var response = await _mediator.Send(request);
            return response.ToActionResult(this);
        }
    }
}
