using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectK.API.Controllers.AuthModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Tests.Security;

public class AuthorizationHttpIntegrationTests
{
    [Theory]
    [InlineData("/api/member/{0}")]
    [InlineData("/api/group/{0}")]
    [InlineData("/api/kurin/{0}")]
    public async Task Anonymous_RequestToProtectedEndpoint_ShouldReturn401(string routeTemplate)
    {
        await using var host = await SecurityTestHost.StartAsync();
        var route = string.Format(routeTemplate, Guid.NewGuid());

        var response = await host.Client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.User, "/api/user/users")]
    [InlineData(UserRole.Mentor, "/api/planning/{0}")]
    [InlineData(UserRole.User, "/api/member/members/kv/{0}")]
    public async Task AuthenticatedUser_WithInsufficientRole_ShouldReturn403(UserRole role, string routeTemplate)
    {
        await using var host = await SecurityTestHost.StartAsync(role);
        var route = routeTemplate.Contains("{0}")
            ? string.Format(routeTemplate, Guid.NewGuid())
            : routeTemplate;

        var response = await host.Client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed class SecurityTestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private SecurityTestHost(WebApplication app)
        {
            _app = app;
            Client = app.GetTestClient();
        }

        public HttpClient Client { get; }

        public static async Task<SecurityTestHost> StartAsync(UserRole? role = null)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });

            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton(new TestAuthState(role));

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin",
                    policy => policy.RequireRole(UserRole.Admin.ToClaimValue()));

                options.AddPolicy("RequireManager",
                    policy => policy.RequireRole(UserRole.Manager.ToClaimValue(), UserRole.Admin.ToClaimValue()));

                options.AddPolicy("RequireMentor",
                    policy => policy.RequireRole(UserRole.Mentor.ToClaimValue(), UserRole.Manager.ToClaimValue(), UserRole.Admin.ToClaimValue()));

                options.AddPolicy("RequireUser",
                    policy => policy.RequireRole(UserRole.User.ToClaimValue(), UserRole.Mentor.ToClaimValue(), UserRole.Manager.ToClaimValue(), UserRole.Admin.ToClaimValue()));
            });

            builder.Services.AddControllers()
                .AddApplicationPart(typeof(AuthController).Assembly);

            var app = builder.Build();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            await app.StartAsync();
            return new SecurityTestHost(app);
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private sealed record TestAuthState(UserRole? Role);

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        private readonly TestAuthState _authState;

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            TestAuthState authState)
            : base(options, logger, encoder)
        {
            _authState = authState;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (_authState.Role is null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, _authState.Role.Value.ToClaimValue())
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    }
}
