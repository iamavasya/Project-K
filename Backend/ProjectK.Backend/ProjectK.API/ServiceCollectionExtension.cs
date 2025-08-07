using Microsoft.Extensions.DependencyInjection;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.Repositories;
using ProjectK.Infrastructure.UnitOfWork;

namespace ProjectK.API
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddProjectDependencies(this IServiceCollection services)
        {
            // Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Repositories
            services.AddScoped<IKurinRepository, KurinRepository>();
            return services;
        }
    }
}
