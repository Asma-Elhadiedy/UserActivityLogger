using System.Text.Encodings.Web;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Logs a user activity entry to the database according to configured <see cref="AuditFailureBehavior"/>.
    /// </summary>
    /// <param name="log">The log entry to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task LogAsync(UserActivityLog log)
    {
        return LogAsync(log, CancellationToken.None);
    }

    /// <summary>
    /// Logs a user activity entry to the database with cancellation support according to configured <see cref="AuditFailureBehavior"/>.
    /// </summary>
    /// <param name="log">The log entry to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LogAsync(UserActivityLog log, CancellationToken cancellationToken)
    {
        var result = await TryLogAsync(log, cancellationToken).ConfigureAwait(false);

        if (result.Canceled && result.Error != null)
        {
            throw result.Error;
        }

        if (!result.Succeeded && _options.FailureBehavior == AuditFailureBehavior.Throw && result.Error != null)
        {
            throw result.Error;
        }
    }

    /// <summary>
    /// Attempts to log a user activity entry and returns an explicit write result without throwing on persistence failure.
    /// </summary>
    /// <param name="log">The log entry to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="AuditWriteResult"/> indicating whether persistence succeeded, failed, or was cancelled.</returns>
    public async Task<AuditWriteResult> TryLogAsync(UserActivityLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);

        if (cancellationToken.IsCancellationRequested)
        {
            var canceledEx = new OperationCanceledException(cancellationToken);
            _logger.LogInformation("Audit logging operation was cancelled before execution for Event '{Event}'.", log.Event);
            return AuditWriteResult.Cancelled(canceledEx);
        }

        try
        {
            _context.Set<UserActivityLog>().Add(log);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return AuditWriteResult.Success;
        }
        catch (OperationCanceledException ex)
        {
            DetachAuditEntry(log);
            _logger.LogInformation("Audit logging operation was cancelled for Event '{Event}'.", log.Event);
            NotifyFailureCallback(log, ex);
            return AuditWriteResult.Cancelled(ex);
        }
        catch (Exception ex)
        {
            DetachAuditEntry(log);

            _logger.LogError(
                ex,
                "Failed to persist user activity log for Event '{Event}', User '{UserId}', Path '{Path}'.",
                log.Event,
                log.UserId ?? "Anonymous",
                log.Path ?? "N/A");

            NotifyFailureCallback(log, ex);
            return AuditWriteResult.Failed(ex);
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

        var cancellationToken = httpContext.RequestAborted;
        await LogAsync(log, cancellationToken).ConfigureAwait(false);
    }

    private void DetachAuditEntry(UserActivityLog log)
    {
        try
        {
            var entry = _context.Entry(log);
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

    private void NotifyFailureCallback(UserActivityLog log, Exception ex)
    {
        if (_options.OnAuditFailed == null)
            return;

        try
        {
            var failureContext = new AuditFailureContext(log, ex, _httpContextAccessor.HttpContext);
            _options.OnAuditFailed(failureContext);
        }
        catch (Exception callbackEx)
        {
            _logger.LogError(callbackEx, "Unhandled exception in UserActivityLoggerOptions.OnAuditFailed callback.");
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
