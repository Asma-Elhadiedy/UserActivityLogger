using System.Text.Encodings.Web;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserActivityLogger.Entities;
using UserActivityLogger.Helpers;
using UserActivityLogger.Options;

namespace UserActivityLogger.Services;

/// <summary>
/// Implementation of <see cref="IUserActivityLogger"/> for logging user activities with Entity Framework Core.
/// </summary>
/// <typeparam name="TContext">The <see cref="DbContext"/> type containing the audit entity configuration.</typeparam>
public class UserActivityLoggerService<TContext> : IUserActivityLogger
    where TContext : DbContext
{
    private readonly ILogger<UserActivityLoggerService<TContext>> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserActivityLoggerOptions _options;
    private readonly IDbContextFactory<TContext>? _contextFactory;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly TContext? _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserActivityLoggerService{TContext}"/> class.
    /// Used by dependency injection to resolve isolated or scoped context dependencies.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UserActivityLoggerService(
        ILogger<UserActivityLoggerService<TContext>> logger,
        IHttpContextAccessor httpContextAccessor,
        UserActivityLoggerOptions options,
        IServiceScopeFactory? scopeFactory = null,
        IDbContextFactory<TContext>? contextFactory = null,
        TContext? context = null)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
        _scopeFactory = scopeFactory;
        _contextFactory = contextFactory;
        _context = context;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserActivityLoggerService{TContext}"/> class with a direct context instance.
    /// Maintained for backwards compatibility.
    /// </summary>
    public UserActivityLoggerService(
        ILogger<UserActivityLoggerService<TContext>> logger,
        TContext context,
        IHttpContextAccessor httpContextAccessor,
        UserActivityLoggerOptions options)
        : this(logger, httpContextAccessor, options, null, null, context)
    {
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    /// <summary>
    /// Logs a user activity entry to the database.
    /// When <see cref="UserActivityLoggerOptions.IsolatePersistence"/> is true (default),
    /// persistence executes on an isolated DbContext instance, ensuring pending business changes are never committed.
    /// </summary>
    /// <param name="log">The log entry to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogAsync(UserActivityLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        try
        {
            if (_options.IsolatePersistence)
            {
                if (_contextFactory != null)
                {
                    await using var isolatedContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    isolatedContext.Set<UserActivityLog>().Add(log);
                    await isolatedContext.SaveChangesAsync().ConfigureAwait(false);
                    return;
                }

                if (_scopeFactory != null)
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var isolatedContext = scope.ServiceProvider.GetService<TContext>();
                    if (isolatedContext != null)
                    {
                        isolatedContext.Set<UserActivityLog>().Add(log);
                        await isolatedContext.SaveChangesAsync().ConfigureAwait(false);
                        return;
                    }
                }
            }

            if (_context != null)
            {
                _context.Set<UserActivityLog>().Add(log);
                await _context.SaveChangesAsync().ConfigureAwait(false);
                return;
            }

            if (_contextFactory != null)
            {
                await using var isolatedContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
                isolatedContext.Set<UserActivityLog>().Add(log);
                await isolatedContext.SaveChangesAsync().ConfigureAwait(false);
                return;
            }

            if (_scopeFactory != null)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var isolatedContext = scope.ServiceProvider.GetService<TContext>();
                if (isolatedContext != null)
                {
                    isolatedContext.Set<UserActivityLog>().Add(log);
                    await isolatedContext.SaveChangesAsync().ConfigureAwait(false);
                    return;
                }
            }

            throw new InvalidOperationException(
                $"No DbContext or factory could be resolved for type '{typeof(TContext).Name}'. " +
                "Ensure that AddDbContext, AddDbContextFactory, or an isolated DbContext is registered.");
        }
        catch (Exception ex)
        {
            if (_context != null)
            {
                DetachAuditEntry(_context, log);
            }

            _logger.LogError(ex, "Failed to save user activity log for Event '{Event}'.", log.Event);
        }
    }

    /// <summary>
    /// Logs a user action with the current HTTP context information.
    /// </summary>
    /// <param name="action">The action description.</param>
    /// <param name="additionalData">Optional additional data to include in the log.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogActionAsync(string action, object? additionalData = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, cannot log user activity");
            return;
        }

        if (ShouldSkipPath(httpContext.Request.Path))
            return;

        string? userId = GetUserId(httpContext);

        if (string.IsNullOrEmpty(userId) && !_options.LogAnonymousUsers)
            return;

        var log = new UserActivityLog
        {
            UserId = userId,
            Event = action,
            Path = BuildFullPath(httpContext),
            Method = httpContext.Request.Method,
            IPAddress = ResolveIpAddress(httpContext),
            AdditionalData = SerializeAdditionalData(additionalData),
            ResponseStatusCode = httpContext.Response.StatusCode,
        };

        await LogAsync(log).ConfigureAwait(false);
    }

    private void DetachAuditEntry(DbContext context, UserActivityLog log)
    {
        try
        {
            var entry = context.Entry(log);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }
        catch (Exception detachEx)
        {
            _logger.LogWarning(detachEx, "Failed to detach uncommitted audit log entity from DbContext change tracker.");
        }
    }

    private string? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(_options.UserIdClaimType);
        return userIdClaim?.Value;
    }

    private string ResolveIpAddress(HttpContext context)
    {
        // Use custom resolver if provided
        if (_options.IpAddressResolver != null)
        {
            var customIp = _options.IpAddressResolver(context);
            if (!string.IsNullOrEmpty(customIp))
                return customIp;
        }

        // Default implementation
        var remoteIpAddress = context.Connection.RemoteIpAddress;
        if (remoteIpAddress == null)
        {
            _logger.LogWarning("Failed to get remote IP address");
            return "Unknown";
        }

        return IpAddressHelper.GetIpAddressString(remoteIpAddress) ?? "Unknown";
    }

    private static string BuildFullPath(HttpContext context)
    {
        var path = context.Request.Path.ToString();

        if (context.Request.QueryString.HasValue)
        {
            var queryString = HttpUtility.UrlDecode(context.Request.QueryString.ToString());
            path += queryString;
        }

        return path;
    }

    private string? SerializeAdditionalData(object? additionalData)
    {
        if (additionalData == null)
            return null;

        try
        {
            return JsonSerializer.Serialize(additionalData, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize additional data");
            return null;
        }
    }

    private bool ShouldSkipPath(PathString path)
    {
        return _options.SkipPaths.Any(skipPath =>
            path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }
}
