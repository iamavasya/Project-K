using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;
using ProjectK.Common.Extensions;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using Microsoft.AspNetCore.Identity.Data;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.RefreshToken;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ProjectK.Common.Models.Enums;
using ProjectK.BusinessLogic.Modules.UsersModule.Command;
using ProjectK.BusinessLogic.Modules.UsersModule.Queries;
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
        [EnableRateLimiting("StrictAuthLimit")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var command = _mapper.Map<RegisterUserCommand>(request);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [EnableRateLimiting("StrictAuthLimit")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            var command = _mapper.Map<LoginUserCommand>(request);
            var response = await _mediator.Send(command);
            if (response.Type == ResultType.Success && response.Data.Tokens != null)
            {
                SetRefreshTokenCookie(response.Data.Tokens.RefreshToken.Token, response.Data.Tokens.RefreshToken.Expires);
            }
            return response.ToActionResult(this);
        }

        public class LoadTestLoginRequest { public string ApiKey { get; set; } = string.Empty; }

        [AllowAnonymous]
        [HttpPost("loadtest-login")]
        public async Task<IActionResult> LoadTestLogin(
            [FromBody] LoadTestLoginRequest request,
            [FromServices] Microsoft.Extensions.Configuration.IConfiguration config,
            [FromServices] Microsoft.AspNetCore.Identity.UserManager<ProjectK.Common.Entities.AuthModule.AppUser> userManager,
            [FromServices] ProjectK.Common.Interfaces.Modules.InfrastructureModule.IJwtService jwtService)
        {
            var expectedKey = config["LoadTestApiKey"];
            if (string.IsNullOrEmpty(expectedKey) || request.ApiKey != expectedKey)
            {
                return Unauthorized(new { message = "Invalid or disabled load test API key." });
            }

            var user = await userManager.FindByEmailAsync("loadtest@projectk.com");
            if (user == null) 
            {
                return NotFound(new { message = "Load test user not found." });
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtService.GenerateAccessToken(user.Id.ToString(), user.Email!, roles, user.KurinKey?.ToString());

            return Ok(new { data = new { accessToken = token } });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshTokens = GetRefreshTokenCookieValues();
            if (refreshTokens.Count == 0)
            {
                return Unauthorized();
            }

            foreach (var refreshToken in refreshTokens.Distinct(StringComparer.Ordinal))
            {
                var command = new RefreshTokenCommand(refreshToken);
                var response = await _mediator.Send(command);
                if (response.Type == ResultType.Success && response.Data?.RefreshToken != null)
                {
                    SetRefreshTokenCookie(response.Data.RefreshToken.Token, response.Data.RefreshToken.Expires);
                    return response.ToActionResult(this);
                }
            }

            DeleteRefreshTokenCookie();
            return Unauthorized();
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
                DeleteRefreshTokenCookie();
            }
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [HttpPost("check-access")]
        public async Task<IActionResult> CheckAccess([FromBody] CheckEntityAccessRequest request)
        {
            var query = new CheckEntityAccessQuery
            {
                EntityType = request.EntityType,
                EntityKey = request.EntityKey,
                Action = request.Action
            };
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpGet("mfa/setup")]
        public async Task<IActionResult> GetMfaSetup()
        {
            var userKeyClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = new GetMfaSetupQuery(Guid.Parse(userKeyClaim!));
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("mfa/enable")]
        public async Task<IActionResult> EnableMfa([FromBody] MfaVerifyRequestDto request)
        {
            var userKeyClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var command = new EnableMfaCommand(Guid.Parse(userKeyClaim!), request.Code);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("mfa/recovery-codes")]
        public async Task<IActionResult> RotateMfaRecoveryCodes([FromBody] MfaRecoveryCodesRequestDto request)
        {
            var userKeyClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var command = new GenerateMfaRecoveryCodesCommand(Guid.Parse(userKeyClaim!), request.CurrentPassword);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        [AllowAnonymous]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("mfa/login-verify")]
        public async Task<IActionResult> VerifyMfaLogin([FromBody] MfaLoginRequestDto request)
        {
            var command = new VerifyMfaLoginCommand(request.Email, request.Code, request.RememberMe);
            var response = await _mediator.Send(command);
            if (response.Type == ResultType.Success && response.Data.Tokens != null)
            {
                SetRefreshTokenCookie(response.Data.Tokens.RefreshToken.Token, response.Data.Tokens.RefreshToken.Expires);
            }
            return response.ToActionResult(this);
        }

        [Authorize(Policy = "RequireUser")]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpGet("mfa/status")]
        public async Task<IActionResult> GetMfaStatus()
        {
            var userKeyClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _mediator.Send(new GetUserQuery(Guid.Parse(userKeyClaim!)));
            return Ok(new { isMfaEnabled = user.Data.TwoFactorEnabled });
        }

        private void SetRefreshTokenCookie(string token, DateTime expires)
        {
            DeleteRefreshTokenCookie();

            var isSecureRequest = IsSecureRequest();
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecureRequest,
                SameSite = isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = expires,
                Path = "/api/auth"
            };
            Response.Cookies.Append(refreshTokenCookieName, token, cookieOptions);
        }

        private void DeleteRefreshTokenCookie()
        {
            var isSecureRequest = IsSecureRequest();

            foreach (var path in new[] { "/api/auth", "/api", "/" })
            {
                Response.Cookies.Delete(refreshTokenCookieName, new CookieOptions
                {
                    Secure = isSecureRequest,
                    SameSite = isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax,
                    Path = path
                });
            }
        }

        private List<string> GetRefreshTokenCookieValues()
        {
            return Request.Headers.Cookie
                .SelectMany(header => header?.Split(';') ?? [])
                .Select(cookie => cookie.Trim())
                .Where(cookie => cookie.StartsWith($"{refreshTokenCookieName}=", StringComparison.Ordinal))
                .Select(cookie => cookie[(refreshTokenCookieName.Length + 1)..])
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Uri.UnescapeDataString(value.Trim('"')))
                .ToList();
        }

        private bool IsSecureRequest()
        {
            return Request.IsHttps
                || string.Equals(Request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase);
        }
    }
}
