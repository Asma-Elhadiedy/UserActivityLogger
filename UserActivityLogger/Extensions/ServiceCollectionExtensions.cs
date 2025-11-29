
namespace UserActivityLogger.Extensions;

public static class ServiceCollectionExtensions
{
    /// 
    /// Adds UserActivityLogger services to the service collection.
    /// 
    /// The DbContext type that contains UserLog entity.
    /// The service collection.
    /// Optional configuration action.
    /// The service collection for chaining.
    public static IServiceCollection AddUserActivityLogger<TContext>(
        this IServiceCollection services,
        Action<UserActivityLoggerOptions>? configure = null)
        where TContext : DbContext
    {
        // Configure options
        var options = new UserActivityLoggerOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Register core services
        services.AddHttpContextAccessor();

        // Register filter
        services.AddScoped<IUserActivityLogger, UserActivityLoggerService<TContext>>();

        return services;
    }

    /// 
    /// Adds the UserActivityLoggingFilter to MVC options.
    /// Call this method inside AddControllersWithViews() or AddControllers() configuration.
    /// 
    /// The MVC options.
    /// The MVC options for chaining.
    public static MvcOptions AddUserActivityLogging(this MvcOptions options)
    {
        options.Filters.AddService<UserActivityLoggingFilter>();
        return options;
    }
}
