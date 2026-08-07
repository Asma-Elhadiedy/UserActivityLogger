
namespace UserActivityLogger.Services;

/// 
/// Implementation of IUserActivityLogger for logging user activities.
/// 
/// The DbContext type that contains UserLog entity.
public class UserActivityLoggerService<TContext> : IUserActivityLogger
    where TContext : DbContext
{
    private readonly ILogger<UserActivityLoggerService<TContext>> _logger;
    private readonly TContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserActivityLoggerOptions _options;


    public UserActivityLoggerService(
        ILogger<UserActivityLoggerService<TContext>> logger,
        TContext context,
        IHttpContextAccessor httpContextAccessor,
        UserActivityLoggerOptions options)
    {
        _logger = logger;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public async Task LogAsync(UserActivityLog log)
    {
        try
        {
            _context.Set<UserActivityLog>().Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user activity log");
        }
    }

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

        // Check if we should log anonymous users
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

        await LogAsync(log);
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