

namespace UserActivityLogger.Services;

/// 
/// Interface for user activity logging service.
/// 
public interface IUserActivityLogger
{
    /// 
    /// Logs a user activity entry to the database.
    /// 
    /// The log entry to save.
    /// A task representing the asynchronous operation.
    Task LogAsync(UserActivityLog log);

    /// 
    /// Logs a user action with the current HTTP context information.
    /// 
    /// The action description.
    /// Optional additional data to include in the log.
    /// A task representing the asynchronous operation.
    Task LogActionAsync(string action, object? additionalData = null);
}

