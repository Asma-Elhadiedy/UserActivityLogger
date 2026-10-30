namespace UserActivityLogger.Services;

/// <summary>
/// Represents the outcome of an audit log persistence operation.
/// </summary>
public readonly record struct AuditWriteResult(
    bool Succeeded,
    Exception? Error = null,
    bool Canceled = false)
{
    /// <summary>
    /// Gets a successful write result.
    /// </summary>
    public static AuditWriteResult Success { get; } = new(true);

    /// <summary>
    /// Creates a failed write result with the associated exception.
    /// </summary>
    /// <param name="error">The exception that caused persistence to fail.</param>
    /// <returns>A failed <see cref="AuditWriteResult"/>.</returns>
    public static AuditWriteResult Failed(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(false, error);
    }

    /// <summary>
    /// Creates a cancelled write result.
    /// </summary>
    /// <param name="error">Optional cancellation exception.</param>
    /// <returns>A cancelled <see cref="AuditWriteResult"/>.</returns>
    public static AuditWriteResult Cancelled(Exception? error = null) => new(false, error, true);
}

