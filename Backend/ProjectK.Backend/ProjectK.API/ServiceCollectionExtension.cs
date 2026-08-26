using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectK.API.Helpers;
using ProjectK.API.Services;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.ProfileVerification;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Settings;
using ProjectK.Infrastructure.Repositories;
using ProjectK.Infrastructure.Repositories.InfrastructureModule;
using ProjectK.Infrastructure.Services;
using ProjectK.Infrastructure.Services.BlobStorageService;
using ProjectK.Infrastructure.Services.EmailService;
using ProjectK.Infrastructure.Services.JwtService;
using ProjectK.Infrastructure.Services.PublicAnnouncements;
using ProjectK.Infrastructure.UnitOfWork;
using Resend;

namespace ProjectK.API
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddProjectDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            // Options
            services.Configure<EmailSettings>(configuration.GetSection("Email"));
            services.Configure<SecurityMonitoringOptions>(configuration.GetSection("SecurityMonitoring"));
            services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));
            services.Configure<PublicAnnouncementImageStoreOptions>(configuration.GetSection("PublicAnnouncements:ImageStore"));
            services.PostConfigure<PublicAnnouncementImageStoreOptions>(options =>
            {
                if (string.IsNullOrWhiteSpace(options.Path))
                {
                    options.Path = configuration["PublicAnnouncements:ImageStorePath"];
                }
            });

            // Services

            // -- Background jobs
            services.AddHostedService<AuditCleanupBackgroundService>();
            services.AddHostedService<MemberWarningExpiryBackgroundService>();

            // -- Data access
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IResourceScopeReader, ResourceScopeReader>();

            // Repositories are reached only through IUnitOfWork, which owns their
            // lifetime and shares the request DbContext. Registering them separately
            // let a second instance exist per request against the same context and
            // nothing resolved them directly, so those registrations were dropped.

            // -- Auth & access control
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IMfaService, MfaService>();
            services.AddScoped<ILoginResponseFactory, LoginResponseFactory>();
            services.AddScoped<ILeadershipRoleSyncService, LeadershipRoleSyncService>();
            services.AddScoped<ISystemSettingsService, SystemSettingsService>();
            services.AddScoped<IMfaEnforcementPolicy, MfaEnforcementPolicy>();
            // Injected wherever the clock decides something — token and invitation expiry, warning
            // windows, the agenda's default range — so those rules can be tested at a fixed instant.
            // Plain timestamps still use DateTime.UtcNow.
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
            services.AddScoped<ResourceAccessService>();
            services.AddScoped<IResourceAccessService>(sp =>
                new ResourceAccessServiceInstrumentationDecorator(
                    sp.GetRequiredService<ResourceAccessService>(),
                    sp.GetRequiredService<ILogger<ResourceAccessServiceInstrumentationDecorator>>()));

            // -- Caching
            services.AddSingleton<IBackendCache, MemoryBackendCache>();

            // -- Kurin module
            services.AddScoped<IMemberProfileVerificationService, MemberProfileVerificationService>();
            services.AddScoped<IAgendaAccess, AgendaAccess>();

            // -- Notifications
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IReviewNotificationRecipientResolver, ReviewNotificationRecipientResolver>();

            // -- Public announcements
            services.AddHttpClient<TelegramPublicAnnouncementPublisher>();
            services.AddScoped<IPublicAnnouncementRenderer, PublicAnnouncementRenderer>();
            services.AddScoped<IPublicAnnouncementImageStore, AzureBlobPublicAnnouncementImageStore>();
            services.AddScoped<NullPublicAnnouncementPublisher>();
            services.AddScoped<IPublicAnnouncementPublisher>(sp =>
                configuration.GetValue<bool>("Telegram:PublicChannel:Enabled")
                    ? sp.GetRequiredService<TelegramPublicAnnouncementPublisher>()
                    : sp.GetRequiredService<NullPublicAnnouncementPublisher>());
            services.AddSingleton<LocalPublicAnnouncementImageStore>();

            // -- Email
            var emailProvider = configuration["Email:Provider"] ?? "Mock";
            if (emailProvider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
            {
                services.AddOptions();
                services.AddHttpClient<IResend, ResendClient>();
                services.Configure<ResendClientOptions>(options =>
                {
                    options.ApiToken = configuration["Email:ApiKey"]!;
                });
                services.AddTransient<IResend, ResendClient>();
                services.AddScoped<IEmailService, ResendEmailService>();
            }
            else
            {
                services.AddScoped<IEmailService, MockEmailService>();
            }

            // -- Probes and badges catalog
            services.AddScoped<IBadgesCatalogService, BadgesCatalogService>();
            services.AddScoped<IProbesCatalogService, ProbesCatalogService>();

            // -- Misc
            services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();
            services.AddSingleton<IActivityLogger, ActivityLogger>();

            return services;
        }
    }
}
