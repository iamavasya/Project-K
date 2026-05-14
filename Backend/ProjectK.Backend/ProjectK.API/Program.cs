using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProjectK.API.MappingProfiles;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.Services.BlobStorageService;
using ProjectK.Infrastructure.Services.BlobStorageService.OrphanCleanup;
using ProjectK.Common.Extensions;
using System.Text;
using System.Threading.RateLimiting;
using ProjectK.API.Helpers;
using System.Security.Claims;
using AutoMapper.EquivalencyExpression;
using ProjectK.Optimization.Extensions;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Get;
using ProjectK.ProbeAndBadges.DependencyInjection;
using ProjectK.ProbeAndBadges.Abstractions;
using Spectre.Console;
using Serilog;

namespace ProjectK.API
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            AnsiConsole.Clear();
            PrintTitle(builder.Configuration);

            builder.Services.AddIdentity<AppUser, AppRole>(options =>
            {
                bool.TryParse(builder.Configuration["DebugMode:SecurePasswordOptions"], out bool securePasswordOption);

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = securePasswordOption;
                options.Password.RequireNonAlphanumeric = securePasswordOption;
                options.Password.RequireUppercase = securePasswordOption;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),

                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
            });

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

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("EnvCorsPolicy", policy =>
                {
                    var frontendUrl = builder.Configuration["EnvCorsOrigin"];
                    if (!string.IsNullOrEmpty(frontendUrl))
                    {
                        policy.WithOrigins(frontendUrl)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                });
            });

            builder.Services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                        b => b.MigrationsAssembly("ProjectK.Infrastructure")
                );
            });

            // --- Blob storage DI ---
            var blobOptions = new BlobStorageOptions
            {
                ConnectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true",
                ContainerName = builder.Configuration["BlobStorage:ContainerName"] ?? "photos",
                PublicAccess = !bool.TryParse(builder.Configuration["BlobStorage:PublicAccess"], out var pa) || pa,
                BlobPrefix = builder.Configuration["BlobStorage:BlobPrefix"],
                PublicBaseUrl = builder.Configuration["BlobStorage:PublicBaseUrl"]
            };
            builder.Services.AddScoped<IPhotoReferenceProvider, MemberPhotoReferenceProvider>();

            builder.Services.AddSingleton(blobOptions);

            builder.Services.Configure<OrphanCleanupOptions>(builder.Configuration.GetSection("OrphanCleanup"));

            builder.Services.AddScoped<IPhotoService, AzureBlobPhotoService>(sp =>
            {
                var opts = sp.GetRequiredService<BlobStorageOptions>();
                var refProvider = sp.GetService<IPhotoReferenceProvider>();
                return new AzureBlobPhotoService(opts, refProvider);
            });

            builder.Services.AddHostedService<OrphanPhotoCleanupService>();
            // --- end Blob storage DI ---

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Global limit
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var bypassKey = builder.Configuration["LoadTestApiKey"];
                    if (!string.IsNullOrEmpty(bypassKey) && 
                        httpContext.Request.Headers.TryGetValue("X-LoadTest-Bypass", out var providedKey) && 
                        providedKey == bypassKey)
                    {
                        return RateLimitPartition.GetNoLimiter("Bypass");
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                // Strict limit for Auth (Login/Register)
                options.AddPolicy("StrictAuthLimit", httpContext =>
                {
                    var bypassKey = builder.Configuration["LoadTestApiKey"];
                    if (!string.IsNullOrEmpty(bypassKey) && 
                        httpContext.Request.Headers.TryGetValue("X-LoadTest-Bypass", out var providedKey) && 
                        providedKey == bypassKey)
                    {
                        return RateLimitPartition.GetNoLimiter("Bypass");
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(5)
                        });
                });

                // Account security endpoints have a lower per-user/IP budget because they perform
                // sensitive verification or token-producing operations.
                options.AddPolicy("AccountSecurityLimit", httpContext =>
                {
                    var bypassKey = builder.Configuration["LoadTestApiKey"];
                    if (!string.IsNullOrEmpty(bypassKey) &&
                        httpContext.Request.Headers.TryGetValue("X-LoadTest-Bypass", out var providedKey) &&
                        providedKey == bypassKey)
                    {
                        return RateLimitPartition.GetNoLimiter("Bypass");
                    }

                    var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: partitionKey,
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(5)
                        });
                });

                options.OnRejected = async (context, token) =>
                {
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                };
            });

            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<ProjectK.API.Services.GeoIPService>();
            builder.Services.AddAutoMapper(cfg => { cfg.AddCollectionMappers(); }, typeof(KurinModuleProfile));
            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(GetKurinByKey).Assembly)
            );
            builder.Services.AddControllers()
                .AddJsonOptions(opt =>
                {
                    opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddWolfPackOptimization();

            builder.Services.Configure<SecurityPatchOptions>(
                builder.Configuration.GetSection("SecurityPatch"));

            builder.Services.AddProbeAndBadgesApi(options =>
            {
                builder.Configuration.GetSection("ProbeAndBadges").Bind(options);
            });

            builder.Services.AddProjectDependencies(builder.Configuration);

            var app = builder.Build();

            await RunStartupTasksAsync(app);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            app.UseCors("EnvCorsPolicy");

            app.UseAuthentication();

            app.UseRateLimiter();
            
            app.UseMiddleware<ProjectK.API.Middleware.SecurityHardeningMiddleware>();
            app.UseMiddleware<ProjectK.API.Middleware.PrivilegedMfaEnforcementMiddleware>();

            app.UseAuthorization();

            app.UseBadgesImagesStaticFiles();

            app.MapControllers();

            app.MapGet("/health", (IConfiguration config) => Results.Ok(new
            {
                status = "ready",
                version = config["ReleaseInfo:Version"] ?? "unknown",
                codeName = config["ReleaseInfo:CodeName"] ?? "unknown",
                utc = DateTimeOffset.UtcNow
            }));

            app.MapGet("/", () => "Backend Started");

            await app.RunAsync();
        }

        private static void PrintTitle(IConfiguration config)
        {
            var version = config["ReleaseInfo:Version"] ?? "v0.0.0";
            var codeName = config["ReleaseInfo:CodeName"] ?? "Unknown";

            AnsiConsole.Write(new FigletText("Project K").Color(Color.Green));
            
            AnsiConsole.Write(new Rule($"[yellow]{version} \"{codeName}\"[/]") 
            { 
                Justification = Justify.Left 
            });
            
            AnsiConsole.WriteLine();

            Thread.Sleep(2000);
        }

        private static async Task RunStartupTasksAsync(WebApplication app)
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("yellow"))
                .StartAsync("Booting the kettle...", async ctx =>
                {
                    using var scope = app.Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    ctx.Status("Summoning database goblins...");
                    await dbContext.Database.MigrateAsync();

                    ctx.Status("Planting heroic seed data...");
                    await DataSeeder.SeedAsync(scope.ServiceProvider);

                    ctx.Status("Waking the badges archive...");
                    _ = scope.ServiceProvider.GetRequiredService<IBadgesCatalog>();

                        ctx.Status("Startup complete.");
                });

                    AnsiConsole.MarkupLine("[green]✔ Startup successful![/]");
                    await Task.Delay(2000);
        }
    }
}
