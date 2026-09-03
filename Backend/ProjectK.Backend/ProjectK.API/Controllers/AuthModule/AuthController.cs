using AutoMapper;
using ProjectK.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;
using ProjectK.Common.Extensions;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using Microsoft.AspNetCore.Identity.Data;
using ProjectK.Common.Models.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.User.Get;
using ProjectK.BusinessLogic.Modules.UsersModule.Features.User.RegisterKurin;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.Access.Check;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.KurinScope.Set;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.RefreshToken.Refresh;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.EnableMfa;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.GenerateMfaRecoveryCodes;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.GetMfaSetup;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.Login;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.Logout;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.Register;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.VerifyMfaLogin;
using ProjectK.API.Authorization;
using ProjectK.BusinessLogic.Modules.UsersModule.Models;

namespace ProjectK.API.Controllers.AuthModule
{
    /// <summary>
    /// Signing in, signing out, and the second factor. Account creation for people who are already known to
    /// the kurin lives here; strangers apply through the waitlist on <c>api/auth/onboarding</c> instead.
    /// </summary>
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

        /// <summary>
        /// Creates a kurin together with its first account, which becomes that kurin's Kurinnyi.
        /// </summary>
        /// <remarks>
        /// Admin only, because it is the one call that brings a new kurin into being. Every other registration
        /// attaches a person to a kurin that already exists.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpPost("register/kurin")]
        [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterKurin([FromBody] RegisterUserRequest request)
        {
            var command = new RegisterKurinCommand
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

        /// <summary>
        /// Creates an account inside the caller's own kurin.
        /// </summary>
        /// <remarks>
        /// Restricted to kurin management and rate-limited: it is the path an attacker would use to mint
        /// accounts if a management token leaked.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireKurinManagement)]
        [EnableRateLimiting("StrictAuthLimit")]
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var command = _mapper.Map<RegisterUserCommand>(request);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Exchanges an email and password for an access token, and sets the refresh cookie.
        /// </summary>
        /// <remarks>
        /// Answers identically for an unknown address and a wrong password, so the response cannot be used to
        /// discover which addresses are registered. When the account has a second factor, the answer carries no
        /// tokens — finish at <c>mfa/login-verify</c>.
        /// </remarks>
        [AllowAnonymous]
        [EnableRateLimiting("StrictAuthLimit")]
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
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

        public class SetKurinScopeRequest { public Guid? KurinKey { get; set; } }

        /// <summary>
        /// Points an administrator's session at a particular kurin and issues tokens scoped to it.
        /// </summary>
        /// <remarks>
        /// Administrators are the only accounts that exist above a single kurin, so their token has to say
        /// which one they are currently acting inside.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
        [HttpPost("kurin-scope")]
        [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetKurinScope([FromBody] SetKurinScopeRequest request)
        {
            if (this.UserKey() is not { } userKey)
            {
                return this.UnreadableIdentity();
            }

            var command = new SetKurinScopeCommand(userKey, request.KurinKey);
            var response = await _mediator.Send(command);
            if (response.Type == ResultType.Success && response.Data?.Tokens != null)
            {
                SetRefreshTokenCookie(response.Data.Tokens.RefreshToken.Token, response.Data.Tokens.RefreshToken.Expires);
            }

            return response.ToActionResult(this);
        }

        public class LoadTestLoginRequest { public string ApiKey { get; set; } = string.Empty; }

        /// <summary>
        /// Mints a token for the load-test account without a password.
        /// </summary>
        /// <remarks>
        /// Guarded by <c>LoadTestLoginKey</c>, which ships blank — a blank key disables the endpoint outright.
        /// It has its own secret rather than sharing the rate limiter's bypass key, so letting a monitor past
        /// the limiter cannot also open a login.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("loadtest-login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> LoadTestLogin(
            [FromBody] LoadTestLoginRequest request,
            [FromServices] Microsoft.Extensions.Configuration.IConfiguration config,
            [FromServices] Microsoft.AspNetCore.Identity.UserManager<ProjectK.Common.Entities.AuthModule.AppUser> userManager,
            [FromServices] ProjectK.Common.Interfaces.Modules.InfrastructureModule.IJwtService jwtService)
        {
            // Its own secret, not the rate limiter's: the two used to share one value, so setting the
            // bypass key to let a monitor through would also have opened a login as the load-test
            // account. Empty means the endpoint is off, which is how it ships.
            var expectedKey = config["LoadTestLoginKey"];
            if (string.IsNullOrEmpty(expectedKey) || request.ApiKey != expectedKey)
            {
                return this.Failure(ResultType.Unauthorized, "InvalidApiKey", "Invalid or disabled load test API key.");
            }

            var user = await userManager.FindByEmailAsync("loadtest@projectk.com");
            if (user == null) 
            {
                return this.Failure(ResultType.NotFound, "UserNotFound", "Load test user not found.");
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtService.GenerateAccessToken(user.Id.ToString(), user.Email!, roles, user.KurinKey?.ToString());

            return Ok(new { data = new { accessToken = token } });
        }

        /// <summary>
        /// Trades the refresh cookie for a fresh access token and rotates the cookie.
        /// </summary>
        /// <remarks>
        /// Walks every refresh cookie the browser sent: a stale cookie from an earlier session otherwise
        /// shadows the current one and logs the user out on reload.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(JwtResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Refresh()
        {
            var refreshTokens = GetRefreshTokenCookieValues();
            if (refreshTokens.Count == 0)
            {
                return this.UnreadableIdentity();
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
            return this.UnreadableIdentity();
        }

        /// <summary>
        /// Revokes the caller's refresh token and clears the cookie.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[refreshTokenCookieName];
            var command = new LogoutUserCommand(this.UserKey()?.ToString(), refreshToken);
            var response = await _mediator.Send(command);
            if (refreshToken != null)
            {
                DeleteRefreshTokenCookie();
            }
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Answers whether the caller may act on one named resource, without performing the action.
        /// </summary>
        /// <remarks>
        /// Lets the frontend hide controls the caller cannot use, using the same decision the endpoint itself
        /// would make rather than a second copy of the rules.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [HttpPost("check-access")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Returns the shared secret and QR payload for enrolling an authenticator app.
        /// </summary>
        /// <remarks>
        /// Enrolment is not finished until <c>mfa/enable</c> confirms a code from that app.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpGet("mfa/setup")]
        [ProducesResponseType(typeof(MfaSetupResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMfaSetup()
        {
            if (this.UserKey() is not { } userKey)
            {
                return this.UnreadableIdentity();
            }

            var query = new GetMfaSetupQuery(userKey);
            var response = await _mediator.Send(query);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Confirms a code from the authenticator app and turns the second factor on.
        /// </summary>
        /// <remarks>
        /// Answers with the recovery codes, which are shown once and never returned again.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("mfa/enable")]
        [ProducesResponseType(typeof(MfaEnableResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> EnableMfa([FromBody] MfaVerifyRequestDto request)
        {
            if (this.UserKey() is not { } userKey)
            {
                return this.UnreadableIdentity();
            }

            var command = new EnableMfaCommand(userKey, request.Code);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Issues a new set of recovery codes and invalidates the previous set.
        /// </summary>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("mfa/recovery-codes")]
        [ProducesResponseType(typeof(MfaRecoveryCodesResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> RotateMfaRecoveryCodes([FromBody] MfaRecoveryCodesRequestDto request)
        {
            if (this.UserKey() is not { } userKey)
            {
                return this.UnreadableIdentity();
            }

            var command = new GenerateMfaRecoveryCodesCommand(userKey, request.CurrentPassword);
            var response = await _mediator.Send(command);
            return response.ToActionResult(this);
        }

        /// <summary>
        /// Completes a sign-in that stopped for the second factor, accepting a code or a recovery code.
        /// </summary>
        /// <remarks>
        /// Anonymous by necessity: the caller has proved the password but has no token yet. Carries the
        /// account-security rate limit, since it is the point where codes could be guessed.
        /// </remarks>
        [AllowAnonymous]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpPost("mfa/login-verify")]
        [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Reports whether the caller has a second factor, and whether their offices require one.
        /// </summary>
        /// <remarks>
        /// Enforcement is a policy decision, not an account setting: holding a privileged office can make the
        /// second factor mandatory for an account that had it switched off.
        /// </remarks>
        [Authorize(Policy = AuthorizationPolicies.RequireUser)]
        [EnableRateLimiting("AccountSecurityLimit")]
        [HttpGet("mfa/status")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMfaStatus([FromServices] IMfaEnforcementPolicy mfaEnforcementPolicy)
        {
            if (this.UserKey() is not { } userKey)
            {
                return this.UnreadableIdentity();
            }

            var user = await _mediator.Send(new GetUserQuery(userKey));
            var isPrivileged = RolePermissionMap.GrantsWholeKurinManagement(
                User.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
            var isMfaRequired = isPrivileged && await mfaEnforcementPolicy.IsPrivilegedMfaRequiredAsync(HttpContext.RequestAborted);
            return Ok(new { isMfaEnabled = user.Data.TwoFactorEnabled, isMfaRequired });
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
