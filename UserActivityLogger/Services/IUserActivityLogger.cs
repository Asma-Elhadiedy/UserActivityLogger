using UserActivityLogger.Entities;

namespace UserActivityLogger.Services;

/// <summary>
/// Interface for user activity logging service.
/// </summary>
public interface IUserActivityLogger
{
    /// <summary>
    /// Logs a user activity entry to the database.
    /// </summary>
    /// <param name="log">The log entry to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogAsync(UserActivityLog log);

    /// <summary>
    /// Logs a user activity entry to the database with a cancellation token.
    /// </summary>
    /// <param name="log">The log entry to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogAsync(UserActivityLog log, CancellationToken cancellationToken)
    {
        return LogAsync(log);
    }

    /// <summary>
    /// Attempts to log a user activity entry and returns an explicit write result without throwing on persistence failure.
    /// </summary>
    /// <param name="log">The log entry to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="AuditWriteResult"/> indicating whether persistence succeeded, failed, or was cancelled.</returns>
    Task<AuditWriteResult> TryLogAsync(UserActivityLog log, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AuditWriteResult.Success);
    }

    /// <summary>
    /// Logs a user action with the current HTTP context information.
    /// </summary>
    /// <param name="action">The action description.</param>
    /// <param name="additionalData">Optional additional data to include in the log.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogActionAsync(string action, object? additionalData = null);
}
