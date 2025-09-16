using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;
using ProjectK.Common.Extensions;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using Microsoft.AspNetCore.Identity.Data;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.RefreshToken;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ProjectK.API.Controllers.AuthModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public AuthController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var command = _mapper.Map<RegisterUserCommand>(request);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            var command = _mapper.Map<LoginUserCommand>(request);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        // TODO: Take refresh token from HttpOnly cookie
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var command = new RefreshTokenCommand(refreshToken);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userKeyClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var command = new LogoutUserCommand(userKeyClaim!);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }
    }
}
