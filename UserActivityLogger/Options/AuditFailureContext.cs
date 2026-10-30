using Microsoft.AspNetCore.Http;
using UserActivityLogger.Entities;

namespace UserActivityLogger.Options;

/// <summary>
/// Provides context information when an audit log persistence operation fails.
/// </summary>
public sealed class AuditFailureContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditFailureContext"/> class.
    /// </summary>
    /// <param name="log">The audit log entry that failed to persist.</param>
    /// <param name="exception">The exception that occurred during persistence.</param>
    /// <param name="httpContext">The current HTTP context, if available.</param>
    public AuditFailureContext(UserActivityLog log, Exception exception, HttpContext? httpContext = null)
    {
        Log = log ?? throw new ArgumentNullException(nameof(log));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        HttpContext = httpContext;
    }

    /// <summary>
    /// Gets the audit log entry that failed to persist.
    /// </summary>
    public UserActivityLog Log { get; }

    /// <summary>
    /// Gets the exception that occurred during persistence.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets the current HTTP context, if available.
    /// </summary>
    public HttpContext? HttpContext { get; }
}

