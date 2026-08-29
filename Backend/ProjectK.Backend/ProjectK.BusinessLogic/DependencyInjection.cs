using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.ProfileVerification;
using ProjectK.BusinessLogic.Modules.KurinModule.Reports;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement;

namespace ProjectK.BusinessLogic;

/// <summary>
/// The domain services this project provides, registered by this project.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services, IConfiguration configuration)
    {
        // Auth and access control
        services.AddScoped<ILoginResponseFactory, LoginResponseFactory>();
        services.AddScoped<ILeadershipRoleSyncService, LeadershipRoleSyncService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IMfaEnforcementPolicy, MfaEnforcementPolicy>();
        services.AddScoped<ResourceAccessService>();
        services.AddScoped<IResourceAccessService>(sp =>
            new ResourceAccessServiceInstrumentationDecorator(
                sp.GetRequiredService<ResourceAccessService>(),
                sp.GetRequiredService<ILogger<ResourceAccessServiceInstrumentationDecorator>>()));

        services.AddSingleton<IBackendCache, MemoryBackendCache>();

        // Kurin module
        services.AddScoped<IMemberProfileVerificationService, MemberProfileVerificationService>();
        services.AddScoped<IAgendaAccess, AgendaAccess>();
        services.AddScoped<KurinReportDataService>();

        // Notifications
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReviewNotificationRecipientResolver, ReviewNotificationRecipientResolver>();

        // Public announcements
        services.AddHttpClient<TelegramPublicAnnouncementPublisher>();
        services.AddScoped<IPublicAnnouncementRenderer, PublicAnnouncementRenderer>();
        services.AddScoped<NullPublicAnnouncementPublisher>();
        services.AddScoped<IPublicAnnouncementPublisher>(sp =>
            configuration.GetValue<bool>("Telegram:PublicChannel:Enabled")
                ? sp.GetRequiredService<TelegramPublicAnnouncementPublisher>()
                : sp.GetRequiredService<NullPublicAnnouncementPublisher>());

        // Probe and badge catalogues
        services.AddScoped<IBadgesCatalogService, BadgesCatalogService>();
        services.AddScoped<IProbesCatalogService, ProbesCatalogService>();

        return services;
    }
}
