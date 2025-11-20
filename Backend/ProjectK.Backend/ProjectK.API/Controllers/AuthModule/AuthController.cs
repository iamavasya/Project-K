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
using ProjectK.Common.Models.Enums;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.Common.Models.Records;
using ProjectK.BusinessLogic.Modules.AuthModule.Queries;

namespace ProjectK.API.Controllers.AuthModule
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private const string refreshTokenCookieName = "refreshToken";

        public AuthController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpPost("register/manager")]
        public async Task<IActionResult> RegisterManager([FromBody] RegisterUserRequest request)
        {
            var command = new RegisterManagerCommand
            {
                Email = request.Email,
                Password = request.Password ?? "tempManagerPass1!",
                FirstName = request.FirstName ?? "tempManagerFirstName",
                LastName = request.LastName ?? "tempManagerLastName",
                PhoneNumber = request.PhoneNumber ?? "tempManagerNumber",
                KurinNumber = (int)request.KurinNumber!
            };
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireManager")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var command = _mapper.Map<RegisterUserCommand>(request);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            var command = _mapper.Map<LoginUserCommand>(request);
            var response = await _mediator.Send(command);
            if (response.Type != ResultType.Unauthorized)
            {
                SetRefreshTokenCookie(response.Data.Tokens.RefreshToken.Token, response.Data.Tokens.RefreshToken.Expires);
            }
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies[refreshTokenCookieName];
            if (refreshToken == null)
            {
                return Unauthorized();
            }
            var command = new RefreshTokenCommand(refreshToken);
            var response = await _mediator.Send(command);
            if (response.Type == ResultType.Unauthorized)
            {
                await Logout();
            }
            else
            {
                SetRefreshTokenCookie(response.Data.RefreshToken.Token, response.Data.RefreshToken.Expires);
            }
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userKeyClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var command = new LogoutUserCommand(userKeyClaim!);
            var response = await _mediator.Send(command);
            var refreshToken = Request.Cookies[refreshTokenCookieName];
            if (refreshToken != null)
            {
                Response.Cookies.Delete(refreshTokenCookieName);
            }
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPost("check-access")]
        public async Task<IActionResult> CheckAccess([FromBody] CheckEntityAccessRequest request)
        {
            var activeKurinKey = string.IsNullOrEmpty(request.ActiveKurinKey) ? User.FindFirstValue("kurinKey") : request.ActiveKurinKey;
            var query = new CheckEntityAccessQuery
            {
                EntityType = request.EntityType,
                EntityKey = request.EntityKey,
                ActiveKurinKey = activeKurinKey
            };
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }

        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expires
            };
            Response.Cookies.Append(refreshTokenCookieName, token, cookieOptions);
        }
    }
}
