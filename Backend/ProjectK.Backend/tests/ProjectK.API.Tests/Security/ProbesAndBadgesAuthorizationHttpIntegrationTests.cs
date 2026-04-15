using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ProjectK.API.Controllers.ProbesAndBadgesModule;
using ProjectK.API.Helpers;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;
using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.API.Tests.Security;

public class ProbesAndBadgesAuthorizationHttpIntegrationTests
{
    [Theory]
    [InlineData("/api/catalog/badges/meta")]
    [InlineData("/api/catalog/probes")]
    [InlineData("/badges_images/test.png")]
    public async Task Anonymous_Request_ShouldReturn401(string route)
    {
        await using var host = await CatalogSecurityTestHost.StartAsync(roleClaim: null);

        var response = await host.Client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/catalog/badges/meta")]
    [InlineData("/api/catalog/probes")]
    public async Task AuthenticatedUser_Request_ShouldReturn200(string route)
    {
        await using var host = await CatalogSecurityTestHost.StartAsync(UserRole.User.ToClaimValue());

        var response = await host.Client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/catalog/badges/meta")]
    [InlineData("/api/catalog/probes")]
    public async Task AuthenticatedUnknownRole_Request_ShouldReturn403(string route)
    {
        await using var host = await CatalogSecurityTestHost.StartAsync(roleClaim: "Guest");

        var response = await host.Client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_BadgesImage_ShouldReturn200()
    {
        await using var host = await CatalogSecurityTestHost.StartAsync(UserRole.User.ToClaimValue());

        var response = await host.Client.GetAsync("/badges_images/test.png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class CatalogSecurityTestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly string _assetsDirectory;

        private CatalogSecurityTestHost(WebApplication app, string assetsDirectory)
        {
            _app = app;
            _assetsDirectory = assetsDirectory;
            Client = app.GetTestClient();
        }

        public HttpClient Client { get; }

        public static async Task<CatalogSecurityTestHost> StartAsync(string? roleClaim)
        {
            var assetsDirectory = Path.Combine(Path.GetTempPath(), "projectk-badges-assets", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(assetsDirectory);
            await File.WriteAllBytesAsync(Path.Combine(assetsDirectory, "test.png"), [0x89, 0x50, 0x4E, 0x47]);

            var badgesCatalogService = new Mock<IBadgesCatalogService>();
            var probesCatalogService = new Mock<IProbesCatalogService>();

            var testBadge = new Badge(
                Id: "badge-1",
                Title: "Badge 1",
                ImagePath: "/badges_images/test.png",
                Country: "UA",
                Specialization: "Default",
                Status: "active",
                Level: 1,
                LastUpdated: "2026-04-15",
                SeekerRequirements: "Do requirements",
                InstructorRequirements: "Confirm requirements",
                FixNotes: []);

            badgesCatalogService.Setup(x => x.GetBadgesMetadata())
                .Returns(new BadgesMetadata(
                    ParserVersion: "1.0",
                    ToolAuthor: "tests",
                    ParserComment: "catalog",
                    ParsedAtUtc: DateTimeOffset.UtcNow,
                    SourceUrl: "https://example.invalid",
                    FixerEnabled: false,
                    FixerMode: "none",
                    TotalBadges: 1));

            badgesCatalogService.Setup(x => x.GetBadges(It.IsAny<int>()))
                .Returns([testBadge]);

            badgesCatalogService.Setup(x => x.GetBadgeById(It.IsAny<string>()))
                .Returns(testBadge);

            probesCatalogService.Setup(x => x.GetProbes())
                .Returns([
                    new ProbeSummaryResponse(
                        Id: "probe-1",
                        Title: "Probe 1",
                        PointsCount: 1,
                        SectionsCount: 0)
                ]);

            probesCatalogService.Setup(x => x.GetGroupedProbeById(It.IsAny<string>()))
                .Returns(new GroupedProbeResponse(
                    Id: "probe-1",
                    Title: "Probe 1",
                    PointsCount: 1,
                    SectionsCount: 0,
                    Sections: []));

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing"
            });

            builder.WebHost.UseTestServer();

            builder.Services.AddSingleton(new CatalogAuthState(roleClaim));
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CatalogAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = CatalogAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, CatalogAuthHandler>(CatalogAuthHandler.SchemeName, _ => { });

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

            builder.Services.AddSingleton(badgesCatalogService.Object);
            builder.Services.AddSingleton(probesCatalogService.Object);
            builder.Services.AddSingleton<IBadgesAssetsStore>(new TestBadgesAssetsStore(assetsDirectory));

            builder.Services.AddControllers()
                .AddApplicationPart(typeof(BadgesCatalogController).Assembly);

            var app = builder.Build();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseBadgesImagesStaticFiles();
            app.MapControllers();

            await app.StartAsync();

            return new CatalogSecurityTestHost(app, assetsDirectory);
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

    private sealed record CatalogAuthState(string? RoleClaim);

    private sealed class CatalogAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "CatalogTest";

        private readonly CatalogAuthState _authState;

        public CatalogAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            CatalogAuthState authState)
            : base(options, logger, encoder)
        {
            _authState = authState;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (string.IsNullOrWhiteSpace(_authState.RoleClaim))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, _authState.RoleClaim)
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

    private sealed class TestBadgesAssetsStore(string assetsDirectoryPath) : IBadgesAssetsStore
    {
        public string? AssetsDirectoryPath { get; } = assetsDirectoryPath;

        public bool HasAssets => true;

        public BadgesAssetsSnapshotInfo? AssetsSnapshot => null;
    }
}
