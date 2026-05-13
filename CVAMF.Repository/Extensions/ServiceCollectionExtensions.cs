using CVAMF.Repository.Interfaces;
using CVAMF.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Adds the Unit of Work pattern to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options => 
    ///     options.UseSqlServer(connectionString));
    /// services.AddUnitOfWork&lt;MyDbContext&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new UnitOfWork.UnitOfWork(context);
        });

        return services;
    }

    /// <summary>
    /// Adds both repositories and Unit of Work to the service collection
    /// </summary>
    /// <typeparam name="TContext">DbContext type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;MyDbContext&gt;(options => 
    ///     options.UseSqlServer(connectionString));
    /// services.AddRepositoriesWithUnitOfWork&lt;MyDbContext&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddRepositoriesWithUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddRepositories();
        services.AddUnitOfWork<TContext>();
        return services;
    }
}
