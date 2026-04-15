using Microsoft.Extensions.DependencyInjection;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.API.Helpers;
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

            // Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
            services.AddScoped<IResourceAccessService, ResourceAccessService>();

            // Repositories
            services.AddScoped<IKurinRepository, KurinRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<ILeadershipRepository, LeadershipRepository>();

            // Probes and badges read-only catalog services.
            services.AddScoped<IBadgesCatalogService, BadgesCatalogService>();
            services.AddScoped<IProbesCatalogService, ProbesCatalogService>();
            return services;
        }
    }
}
