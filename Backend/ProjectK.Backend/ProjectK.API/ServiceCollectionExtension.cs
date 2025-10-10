using Microsoft.Extensions.DependencyInjection;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.Repositories;
using ProjectK.Infrastructure.Services.JwtService;
using ProjectK.Infrastructure.UnitOfWork;

namespace ProjectK.API
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddProjectDependencies(this IServiceCollection services)
        {
            // Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtService, JwtService>();

            // Repositories
            services.AddScoped<IKurinRepository, KurinRepository>();
            services.AddScoped<IGroupRepository, GroupRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<ILeadershipRepository, LeadershipRepository>();
            return services;
        }
    }
}
