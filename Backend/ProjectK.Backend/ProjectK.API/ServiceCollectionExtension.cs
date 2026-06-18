using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.ProfileVerification;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.API.Helpers;
using ProjectK.API.Services;
using ProjectK.Infrastructure.Repositories;
using ProjectK.Infrastructure.Services.JwtService;
using ProjectK.Infrastructure.UnitOfWork;

using Microsoft.Extensions.Configuration;
using ProjectK.Common.Models.Settings;
using ProjectK.Infrastructure.Services.BlobStorageService;
using ProjectK.Infrastructure.Services.EmailService;
using ProjectK.Infrastructure.Services.PublicAnnouncements;
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

            // Background Services
            services.AddHostedService<AuditCleanupBackgroundService>();
            services.AddHostedService<MemberWarningExpiryBackgroundService>();

            // Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IMfaService, ProjectK.Infrastructure.Services.MfaService>();
            services.AddScoped<ILoginResponseFactory, LoginResponseFactory>();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
            services.AddSingleton<IActivityLogger, ActivityLogger>();
            services.AddScoped<IPublicAnnouncementRenderer, PublicAnnouncementRenderer>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IReviewNotificationRecipientResolver, ReviewNotificationRecipientResolver>();
            services.Configure<PublicAnnouncementImageStoreOptions>(configuration.GetSection("PublicAnnouncements:ImageStore"));
            services.PostConfigure<PublicAnnouncementImageStoreOptions>(options =>
            {
                if (string.IsNullOrWhiteSpace(options.Path))
                {
                    options.Path = configuration["PublicAnnouncements:ImageStorePath"];
                }
            });
            services.AddSingleton<LocalPublicAnnouncementImageStore>();
            services.AddScoped<IPublicAnnouncementImageStore, AzureBlobPublicAnnouncementImageStore>();
            services.AddScoped<NullPublicAnnouncementPublisher>();
            services.AddHttpClient<TelegramPublicAnnouncementPublisher>();
            services.AddScoped<IPublicAnnouncementPublisher>(sp =>
                configuration.GetValue<bool>("Telegram:PublicChannel:Enabled")
                    ? sp.GetRequiredService<TelegramPublicAnnouncementPublisher>()
                    : sp.GetRequiredService<NullPublicAnnouncementPublisher>());

            // Email Service Registration
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

            services.AddScoped<ResourceAccessService>();
            services.AddScoped<MemberProfileVerificationService>();
            services.AddScoped<IResourceAccessService>(sp =>
                new ResourceAccessServiceInstrumentationDecorator(
                    sp.GetRequiredService<ResourceAccessService>(),
                    sp.GetRequiredService<ILogger<ResourceAccessServiceInstrumentationDecorator>>()));
            services.AddSingleton<IBackendCache, MemoryBackendCache>();

            // Repositories
            services.AddScoped<IKurinRepository, KurinRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<ILeadershipRepository, LeadershipRepository>();
            services.AddScoped<IMemberWarningRepository, MemberWarningRepository>();
            services.AddScoped<IBadgeProgressRepository, BadgeProgressRepository>();
            services.AddScoped<IProbeProgressRepository, ProbeProgressRepository>();
            services.AddScoped<IAppNotificationRepository, AppNotificationRepository>();

            // Probes and badges read-only catalog services.
            services.AddScoped<IBadgesCatalogService, BadgesCatalogService>();
            services.AddScoped<IProbesCatalogService, ProbesCatalogService>();
            return services;
        }
    }
}
