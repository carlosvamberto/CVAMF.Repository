using CVAMF.Repository.Interfaces;
using CVAMF.Repository.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CVAMF.Repository.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register repositories
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the generic repository to the service collection
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        return services;
    }
}
