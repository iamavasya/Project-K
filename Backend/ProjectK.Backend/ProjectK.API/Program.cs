using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ProjectK.API.Helpers;
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
using System.Security.Claims;
using AutoMapper.EquivalencyExpression;
using ProjectK.Optimization.Extensions;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Get;
using ProjectK.ProbeAndBadges.DependencyInjection;
using ProjectK.ProbeAndBadges.Abstractions;
using Spectre.Console;
using Serilog;
using Serilog.Enrichers.Sensitive;
using Serilog.Filters.Expressions;
using Microsoft.OpenApi;
using ProjectK.API.Services.TelegramDevAlerts;
using ProjectK.Common.Models.Settings;
using ProjectK.API.Services.Authorization;
using ProjectK.API.Services.Reports;
using QuestPDF.Infrastructure;

namespace ProjectK.API
{
    public static class Program
    {
        private const string UnknownValue = "unknown";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureQuestPdfLicense(builder.Configuration);

            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithSensitiveDataMasking(_ => { });

                var devAlerts = context.Configuration
                    .GetSection("Telegram:DevAlerts")
                    .Get<TelegramDevAlertOptions>() ?? new TelegramDevAlertOptions();

                if (devAlerts.Enabled)
                {
                    configuration.WriteTo.Logger(lc => lc
                        .Filter.ByIncludingOnly("EventType = 'Security.Suspicious' or @Level >= 'Error'")
                        .WriteTo.TelegramDevAlerts(
                            devAlerts,
                            context.HostingEnvironment.EnvironmentName,
                            context.Configuration["ReleaseInfo:Version"] ?? UnknownValue,
                            context.Configuration["ReleaseInfo:Codename"]
                                ?? context.Configuration["ReleaseInfo:CodeName"]
                                ?? UnknownValue));
                }
            });

            TryClearConsole();
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

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IAuthorizationHandler, AdminOrServiceTokenHandler>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin",
                    policy => policy.RequireRole(UserRole.Admin.ToClaimValue()));

                options.AddPolicy(AdminOrServiceTokenRequirement.PolicyName,
                    policy => policy.AddRequirements(new AdminOrServiceTokenRequirement()));

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
                PublicBaseUrl = builder.Configuration["BlobStorage:PublicBaseUrl"]
            };
            builder.Services.AddScoped<MemberPhotoReferenceProvider>();
            builder.Services.AddScoped<GroupSilhouetteReferenceProvider>();
            builder.Services.AddScoped<PublicAnnouncementImageReferenceProvider>();
            builder.Services.AddScoped<IPhotoReferenceProvider>(sp =>
            {
                var providers = new IPhotoReferenceProvider[]
                {
                    sp.GetRequiredService<MemberPhotoReferenceProvider>(),
                    sp.GetRequiredService<GroupSilhouetteReferenceProvider>(),
                    sp.GetRequiredService<PublicAnnouncementImageReferenceProvider>()
                };

                return new CompositePhotoReferenceProvider(providers);
            });

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

            ConfigureRateLimiting(builder.Services, builder.Configuration);

            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<ProjectK.API.Services.GeoIPService>();
            builder.Services.AddScoped<KurinReportDataService>();
            builder.Services.AddScoped<KurinReportMediaService>();
            builder.Services.AddSingleton<KurinReportPdfRenderer>();
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
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            builder.Services.AddWolfPackOptimization();

            builder.Services.Configure<SecurityPatchOptions>(
                builder.Configuration.GetSection("SecurityPatch"));

            builder.Services.AddProbeAndBadgesApi(options =>
            {
                builder.Configuration.GetSection("ProbeAndBadges").Bind(options);
            });

            builder.Services.AddProjectDependencies(builder.Configuration);

            builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            app.UseForwardedHeaders();

            ValidateTelegramConfiguration(app);

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
            app.UseMiddleware<ProjectK.API.Middleware.SecurityActivityMiddleware>();
            app.UseMiddleware<ProjectK.API.Middleware.PrivilegedMfaEnforcementMiddleware>();

            app.UseAuthorization();

            app.UseBadgesImagesStaticFiles();

            app.MapControllers();

            app.MapGet("/health", (IConfiguration config) => Results.Ok(new
            {
                status = "ready",
                version = config["ReleaseInfo:Version"] ?? UnknownValue,
                codeName = config["ReleaseInfo:Codename"] ?? config["ReleaseInfo:CodeName"] ?? UnknownValue,
                utc = DateTimeOffset.UtcNow
            }));

            app.MapGet("/", () => "Backend Started");

            await app.RunAsync();
        }

        private static void PrintTitle(IConfiguration config)
        {
            var version = config["ReleaseInfo:Version"] ?? "v0.0.0";
            var codeName = config["ReleaseInfo:Codename"] ?? config["ReleaseInfo:CodeName"] ?? "Unknown";

            AnsiConsole.Write(new FigletText("Project K").Color(Spectre.Console.Color.Green));
            
            AnsiConsole.Write(new Rule($"[yellow]{version} \"{codeName}\"[/]") 
            { 
                Justification = Justify.Left 
            });
            
            AnsiConsole.WriteLine();

            Thread.Sleep(2000);
        }

        private static void ConfigureQuestPdfLicense(IConfiguration configuration)
        {
            QuestPDF.Settings.License = Enum.TryParse<LicenseType>(
                configuration["QuestPdf:License"],
                ignoreCase: true,
                out var license)
                    ? license
                    : LicenseType.Community;
        }

        private static void ValidateTelegramConfiguration(WebApplication app)
        {
            var devAlerts = app.Configuration
                .GetSection("Telegram:DevAlerts")
                .Get<TelegramDevAlertOptions>() ?? new TelegramDevAlertOptions();

            if (devAlerts.Enabled && (string.IsNullOrWhiteSpace(devAlerts.BotToken) || string.IsNullOrWhiteSpace(devAlerts.ChatId)))
            {
                app.Logger.LogWarning(
                    "Telegram dev alerts are enabled but BotToken or ChatId is missing. Alerts will not be delivered.");
            }
        }

        private static void TryClearConsole()
        {
            try
            {
                AnsiConsole.Clear();
            }
            catch (IOException)
            {
                // Some CI/service hosts expose stdout without an interactive console buffer.
            }
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

        private static void ConfigureRateLimiting(IServiceCollection services, IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var bypassPartition = TryGetBypassPartition(httpContext, configuration);
                    if (bypassPartition is not null)
                    {
                        return bypassPartition.Value;
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 300,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                options.AddPolicy<string>("StrictAuthLimit", httpContext =>
                {
                    var bypassPartition = TryGetBypassPartition(httpContext, configuration);
                    if (bypassPartition is not null)
                    {
                        return bypassPartition.Value;
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

                options.AddPolicy<string>("AccountSecurityLimit", httpContext =>
                {
                    var bypassPartition = TryGetBypassPartition(httpContext, configuration);
                    if (bypassPartition is not null)
                    {
                        return bypassPartition.Value;
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
                    var endpoint = context.HttpContext.GetEndpoint();
                    var policyName = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                    var activityLogger = context.HttpContext.RequestServices.GetService<IActivityLogger>();
                    activityLogger?.ReportRateLimitRejection(policyName);
                    await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
                };
            });
        }

        private static RateLimitPartition<string>? TryGetBypassPartition(HttpContext httpContext, IConfiguration configuration)
        {
            var bypassKey = configuration["RateLimitBypassKey"];
            if (!string.IsNullOrEmpty(bypassKey) &&
                httpContext.Request.Headers.TryGetValue("X-RateLimit-Bypass", out var providedKey) &&
                providedKey == bypassKey)
            {
                return RateLimitPartition.GetNoLimiter("Bypass");
            }

            return null;
        }
    }
}
