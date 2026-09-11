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
using ProjectK.BusinessLogic.Services.Events;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

namespace ProjectK.BusinessLogic;

/// <summary>
/// The domain services this project provides, registered by this project.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services, IConfiguration configuration)
    {
        // Auth and access control
        services.AddScoped<IAccessContextResolver, AccessContextResolver>();
        services.AddScoped<ILoginResponseFactory, LoginResponseFactory>();
        services.AddScoped<IAccountProvisioningService, AccountProvisioningService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IMfaEnforcementPolicy, MfaEnforcementPolicy>();
        services.AddScoped<ResourceAccessService>();
        services.AddScoped<IResourceAccessService>(sp =>
            new ResourceAccessServiceInstrumentationDecorator(
                sp.GetRequiredService<ResourceAccessService>(),
                sp.GetRequiredService<ILogger<ResourceAccessServiceInstrumentationDecorator>>()));

        services.AddSingleton<IBackendCache, MemoryBackendCache>();

        // Domain events. The in-process delivery is the only thing a broker would replace.
        services.AddScoped<IDomainEventPublisher, InProcessDomainEventPublisher>();

        services.AddMemberModule();

        // Kurin module
        services.AddScoped<IAgendaAccess, AgendaAccess>();
        services.AddScoped<KurinReportDataService>();

        // Notifications
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReviewNotificationRecipientResolver, ReviewNotificationRecipientResolver>();

        // Probe and badge catalogues
        services.AddScoped<IBadgesCatalogService, BadgesCatalogService>();
        services.AddScoped<IProbesCatalogService, ProbesCatalogService>();

        // What each module will answer about a person on someone else's behalf.
        services.AddScoped<IMembershipDirectory, MembershipDirectory>();
        services.AddScoped<IOfficeDirectory, OfficeDirectory>();
        services.AddScoped<IMemberProgressDirectory, MemberProgressDirectory>();

        return services;
    }

    /// <summary>
    /// The member module registers itself. Everything a person is made of is behind this one call —
    /// deleting the line takes the module out and breaks nothing else at compile time except the
    /// contract other modules hold, which is the point.
    /// </summary>
    private static IServiceCollection AddMemberModule(this IServiceCollection services)
    {
        services.AddScoped<IMemberDirectory, MemberDirectory>();
        services.AddScoped<IMemberProfileVerificationService, MemberProfileVerificationService>();
        return services;
    }
}
