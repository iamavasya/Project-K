using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
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
using ProjectK.API.Helpers;
using System.Security.Claims;
using AutoMapper.EquivalencyExpression;
using ProjectK.Optimization.Extensions;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Get;
using ProjectK.ProbeAndBadges.DependencyInjection;

namespace ProjectK.API
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });

                options.AddPolicy("TailscalePolicy", policy =>
                {
                    var frontendUrl = builder.Configuration["TailscaleCorsOrigin"];

                    policy.WithOrigins(frontendUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });

                options.AddPolicy("ProdCorsPolicy", policy =>
                {
                    var frontendUrl = builder.Configuration["ProdCorsOrigin"];

                    policy.WithOrigins(frontendUrl)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
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

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();

                await DataSeeder.SeedAsync(scope.ServiceProvider);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();

            if (app.Environment.IsEnvironment("Tailscale")) app.UseCors("TailscalePolicy");
            else if (app.Environment.IsEnvironment("Production")) app.UseCors("ProdCorsPolicy");
            else app.UseCors("AllowFrontend");

            app.UseAuthentication();

            if (!app.Environment.IsEnvironment("Production"))
            {
                app.Use(async (context, next) =>
                {
                    if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
                    {
                        Console.WriteLine("User is authenticated!");
                        foreach (var claim in context.User.Claims)
                        {
                            Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("User is NOT authenticated.");
                    }

                    await next();
                });
            }

            app.UseAuthorization();

            app.UseBadgesImagesStaticFiles();

            app.MapControllers();

            app.MapGet("/", () => "Backend Started");

            await app.RunAsync();
        }
    }
}
