using Microsoft.Extensions.DependencyInjection;
using ProjectK.Optimization.Abstractions;
using ProjectK.Optimization.Algorithms;

namespace ProjectK.Optimization.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWolfPackOptimization(this IServiceCollection services)
    {
        // Реєструємо GWO як імплементацію IOptimizer
        services.AddTransient<IOptimizer, GreyWolfOptimizer>();
        return services;
    }
}