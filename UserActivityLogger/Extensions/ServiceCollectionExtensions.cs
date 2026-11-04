using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserActivityLogger.Entities;
using UserActivityLogger.Filters;
using UserActivityLogger.Options;
using UserActivityLogger.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UserActivityLogger.Extensions;

/// <summary>
/// Extension methods for setting up UserActivityLogger services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds UserActivityLogger services to the service collection.
    /// By default, persistence runs in an isolated DbContext instance to prevent committing pending business entities.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type that contains the UserActivityLog entity configuration.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUserActivityLogger<TContext>(
        this IServiceCollection services,
        Action<UserActivityLoggerOptions>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        bool optionsRegistered = services.Any(
            service => service.ServiceType == typeof(UserActivityLoggerOptions)
        );

        if (optionsRegistered)
        {
            if (configure != null)
            {
                throw new InvalidOperationException(
                    "UserActivityLogger is already configured."
                );
            }

            return services;
        }

        // Configure options
        var options = new UserActivityLoggerOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Register core services
        services.AddHttpContextAccessor();

        // Register filter and logging service
        services.TryAddScoped<IUserActivityLogger, UserActivityLoggerService<TContext>>();
        services.TryAddScoped<UserActivityLoggingFilter>();

        return services;
    }

    /// <summary>
    /// Adds UserActivityLogger services with isolated persistence.
    /// Ensures audit persistence executes on an independent DbContext instance without touching the host application's scoped unit of work.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIsolatedUserActivityLogger<TContext>(
        this IServiceCollection services,
        Action<UserActivityLoggerOptions>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddUserActivityLogger<TContext>(options =>
        {
            options.IsolatePersistence = true;
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Adds UserActivityLogger services using a dedicated <see cref="AuditDbContext"/> with an isolated context factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dbContextOptions">Action to configure options for the dedicated <see cref="AuditDbContext"/>.</param>
    /// <param name="configure">Optional configuration action for UserActivityLogger options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDedicatedUserActivityLogger(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbContextOptions,
        Action<UserActivityLoggerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dbContextOptions);

        services.AddDbContextFactory<AuditDbContext>(dbContextOptions);
        return services.AddUserActivityLogger<AuditDbContext>(configure);
    }

    /// <summary>
    /// Adds UserActivityLogger services using a dedicated <see cref="AuditDbContext"/> with an isolated context factory configured with service provider access.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dbContextOptions">Action to configure options with access to the service provider.</param>
    /// <param name="configure">Optional configuration action for UserActivityLogger options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDedicatedUserActivityLogger(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptions,
        Action<UserActivityLoggerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dbContextOptions);

        services.AddDbContextFactory<AuditDbContext>(dbContextOptions);
        return services.AddUserActivityLogger<AuditDbContext>(configure);
    }

    /// <summary>
    /// Adds the UserActivityLoggingFilter to MVC options.
    /// Call this method inside AddControllersWithViews() or AddControllers() configuration.
    /// </summary>
    /// <param name="options">The MVC options.</param>
    /// <returns>The MVC options for chaining.</returns>
    public static MvcOptions AddUserActivityLogging(this MvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        bool filterExists = options.Filters
            .OfType<ServiceFilterAttribute>()
            .Any(filter =>
                filter.ServiceType == typeof(UserActivityLoggingFilter));

        if (!filterExists)
        {
            options.Filters.AddService<UserActivityLoggingFilter>();
        }
        return options;
    }
}
