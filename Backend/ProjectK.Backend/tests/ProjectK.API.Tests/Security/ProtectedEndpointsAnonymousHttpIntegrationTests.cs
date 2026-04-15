using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectK.API.Controllers.AuthModule;
using ProjectK.API.Helpers;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;
using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.API.Tests.Security;

public class ProtectedEndpointsAnonymousHttpIntegrationTests
{
    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task Anonymous_RequestToProtectedEndpoint_ShouldReturn401(string method, string route, string? body)
    {
        await using var host = await ProtectedEndpointsTestHost.StartAsync();

        var response = await SendAsync(host.Client, method, route, body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static IEnumerable<object[]> ProtectedRoutes()
    {
        var g = "11111111-1111-1111-1111-111111111111";

        // Auth module
        yield return Row("POST", "/api/auth/logout");
        yield return Row("POST", "/api/auth/register", "{}");
        yield return Row("POST", "/api/auth/register/manager", "{}");
        yield return Row("POST", "/api/auth/check-access", "{}");

        // Users module
        yield return Row("GET", "/api/user/users");

        // Member module
        yield return Row("GET", $"/api/member/{g}");
        yield return Row("GET", $"/api/member/groups/{g}/members");
        yield return Row("GET", $"/api/member/kurins/{g}/members");
        yield return Row("POST", "/api/member", "{}");
        yield return Row("PUT", $"/api/member/{g}", "{}");
        yield return Row("DELETE", $"/api/member/{g}");
        yield return Row("GET", $"/api/member/members/kv/{g}");

        // Group module
        yield return Row("GET", $"/api/group/{g}");
        yield return Row("GET", $"/api/group/exists/{g}");
        yield return Row("GET", $"/api/group/groups?kurinKey={g}");
        yield return Row("POST", "/api/group", "{}");
        yield return Row("PUT", $"/api/group/{g}", "{}");
        yield return Row("DELETE", $"/api/group/{g}");

        // Kurin module
        yield return Row("GET", $"/api/kurin/{g}");
        yield return Row("GET", "/api/kurin/kurins");
        yield return Row("POST", "/api/kurin", "1");
        yield return Row("PUT", $"/api/kurin/{g}", "1");
        yield return Row("DELETE", $"/api/kurin/{g}");

        // Leadership module
        yield return Row("GET", $"/api/leadership/type/kurin/{g}");
        yield return Row("GET", $"/api/leadership/{g}");
        yield return Row("POST", "/api/leadership", "{}");
        yield return Row("PUT", $"/api/leadership/{g}", "{}");
        yield return Row("GET", $"/api/leadership/histories/{g}");

        // Planning module
        yield return Row("POST", "/api/planning", "{}");
        yield return Row("GET", $"/api/planning/session/{g}");
        yield return Row("GET", $"/api/planning/{g}");
        yield return Row("DELETE", $"/api/planning/{g}");

        // Probes and badges catalog module
        yield return Row("GET", "/api/catalog/badges/meta");
        yield return Row("GET", "/api/catalog/badges");
        yield return Row("GET", "/api/catalog/badges/badge-1");
        yield return Row("GET", "/api/catalog/probes");
        yield return Row("GET", "/api/catalog/probes/probe-1/grouped");

        // Member progress module
        yield return Row("GET", $"/api/member/{g}/badges/progress");
        yield return Row("POST", $"/api/member/{g}/badges/badge-1/submit", "{}");
        yield return Row("POST", $"/api/member/{g}/badges/badge-1/review", "{}");
        yield return Row("GET", $"/api/member/{g}/probes/probe-1/progress");
        yield return Row("PUT", $"/api/member/{g}/probes/probe-1/progress/status", "{}");

        // Protected static assets
        yield return Row("GET", "/badges_images/test.png");
    }

    private static object[] Row(string method, string route, string? body = null) => [method, route, body];

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string route, string? body)
    {
        if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            return client.GetAsync(route);
        }

        if (string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
        {
            return client.DeleteAsync(route);
        }

        if (IsMultipartMemberEndpoint(method, route))
        {
            var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent(Guid.NewGuid().ToString()), "GroupKey");
            multipart.Add(new StringContent("Test"), "FirstName");
            multipart.Add(new StringContent("User"), "LastName");
            multipart.Add(new StringContent("Middle"), "MiddleName");
            multipart.Add(new StringContent("test@example.com"), "Email");
            multipart.Add(new StringContent("+380000000000"), "PhoneNumber");
            multipart.Add(new StringContent("2000-01-01"), "DateOfBirth");

            var multipartRequest = new HttpRequestMessage(new HttpMethod(method), route)
            {
                Content = multipart
            };

            return client.SendAsync(multipartRequest);
        }

        var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = new StringContent(body ?? "{}", Encoding.UTF8, "application/json")
        };

        return client.SendAsync(request);
    }

    private static bool IsMultipartMemberEndpoint(string method, string route)
    {
        var isMethod = string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
            || string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase);

        if (!isMethod)
        {
            return false;
        }

        if (!route.StartsWith("/api/member", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !route.Contains("/badges/", StringComparison.OrdinalIgnoreCase)
            && !route.Contains("/probes/", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ProtectedEndpointsTestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly string _assetsDirectory;

        private ProtectedEndpointsTestHost(WebApplication app, string assetsDirectory)
        {
            _app = app;
            _assetsDirectory = assetsDirectory;
            Client = app.GetTestClient();
        }

        public HttpClient Client { get; }

        public static async Task<ProtectedEndpointsTestHost> StartAsync()
        {
            var assetsDirectory = Path.Combine(Path.GetTempPath(), "projectk-badges-assets", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(assetsDirectory);
            await File.WriteAllBytesAsync(Path.Combine(assetsDirectory, "test.png"), [0x89, 0x50, 0x4E, 0x47]);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });

            builder.WebHost.UseTestServer();

            builder.Services.AddSingleton(new AnonymousAuthState());

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = AnonymousAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = AnonymousAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, AnonymousAuthHandler>(AnonymousAuthHandler.SchemeName, _ => { });

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

            builder.Services.Configure<SecurityPatchOptions>(options =>
            {
                options.EnableResourceGuard = true;
            });

            builder.Services.AddSingleton<IBadgesAssetsStore>(new TestBadgesAssetsStore(assetsDirectory));
            builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);

            var app = builder.Build();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseBadgesImagesStaticFiles();
            app.MapControllers();

            await app.StartAsync();
            return new ProtectedEndpointsTestHost(app, assetsDirectory);
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();

            if (Directory.Exists(_assetsDirectory))
            {
                Directory.Delete(_assetsDirectory, recursive: true);
            }
        }
    }

    private sealed record AnonymousAuthState;

    private sealed class AnonymousAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "AnonymousOnly";

        public AnonymousAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            AnonymousAuthState authState)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Task.FromResult(AuthenticateResult.NoResult());
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

    private sealed class TestBadgesAssetsStore(string assetsDirectoryPath) : IBadgesAssetsStore
    {
        public string? AssetsDirectoryPath { get; } = assetsDirectoryPath;
        public bool HasAssets => true;
        public BadgesAssetsSnapshotInfo? AssetsSnapshot => null;
    }
}
