using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.API.Helpers;
using ProjectK.API.Services;
using ProjectK.Infrastructure.Repositories;
using ProjectK.Infrastructure.Services.JwtService;
using ProjectK.Infrastructure.UnitOfWork;

namespace ProjectK.API
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddProjectDependencies(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            // Background Services
            services.AddHostedService<AuditCleanupBackgroundService>();

            // Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
            services.AddScoped<IEmailService, MockEmailService>();
            
            services.AddScoped<ResourceAccessService>();
            services.AddScoped<IResourceAccessService>(sp => 
                new ResourceAccessServiceInstrumentationDecorator(
                    sp.GetRequiredService<ResourceAccessService>(),
                    sp.GetRequiredService<ILogger<ResourceAccessServiceInstrumentationDecorator>>()));

            // Repositories
            services.AddScoped<IKurinRepository, KurinRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<ILeadershipRepository, LeadershipRepository>();
            services.AddScoped<IBadgeProgressRepository, BadgeProgressRepository>();
            services.AddScoped<IProbeProgressRepository, ProbeProgressRepository>();

            // Probes and badges read-only catalog services.
            services.AddScoped<IBadgesCatalogService, BadgesCatalogService>();
            services.AddScoped<IProbesCatalogService, ProbesCatalogService>();
            return services;
        }
    }
}
