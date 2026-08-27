using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.BackgroundServices;
using ProjectK.Infrastructure.Logging;
using ProjectK.Infrastructure.Reports;
using ProjectK.Infrastructure.Repositories;
using ProjectK.Infrastructure.Repositories.InfrastructureModule;
using ProjectK.Infrastructure.Services;
using ProjectK.Infrastructure.Seeding;
using ProjectK.Infrastructure.Services.EmailService;
using ProjectK.Infrastructure.Services.GeoIP;
using ProjectK.Infrastructure.Services.JwtService;
using ProjectK.Infrastructure.Services.PublicAnnouncements;
using ProjectK.Infrastructure.UnitOfWork;
using Resend;
using ProjectK.Infrastructure.Repositories.AuthModule;
using ProjectK.Infrastructure.Repositories.KurinModule;
using ProjectK.Infrastructure.Repositories.ProbesAndBadgesModule;

namespace ProjectK.Infrastructure;

/// <summary>
/// Everything this project provides, registered by this project. The API used to list registrations
/// for all three layers in one file, so adding a service meant editing a project that had no other
/// reason to know about it.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Data access. Repositories are reached only through IUnitOfWork, which owns their lifetime
        // and shares the request DbContext, so they are deliberately not registered individually.
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IResourceScopeReader, ResourceScopeReader>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IMfaService, MfaService>();

        services.AddHostedService<AuditCleanupBackgroundService>();
        services.AddHostedService<MemberWarningExpiryBackgroundService>();

        services.AddScoped<IPublicAnnouncementImageStore, AzureBlobPublicAnnouncementImageStore>();
        services.AddSingleton<LocalPublicAnnouncementImageStore>();

        services.AddScoped<IKurinReportSource, KurinReportSource>();
        services.AddScoped<IKurinReportMedia, KurinReportMediaService>();
        services.AddSingleton<KurinReportPdfRenderer>();

        services.AddScoped<GeoIPService>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();
        services.AddSingleton<IActivityLogger, ActivityLogger>();

        AddEmail(services, configuration);

        return services;
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Email:Provider"] ?? "Mock";
        if (!provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailService, MockEmailService>();
            return;
        }

        services.AddOptions();
        services.AddHttpClient<IResend, ResendClient>();
        services.Configure<ResendClientOptions>(options => options.ApiToken = configuration["Email:ApiKey"]!);
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailService, ResendEmailService>();
    }
}
