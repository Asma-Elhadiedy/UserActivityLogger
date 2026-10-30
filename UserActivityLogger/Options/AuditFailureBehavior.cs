namespace UserActivityLogger.Options;

/// <summary>
/// Specifies how persistence failures are handled during audit logging.
/// </summary>
public enum AuditFailureBehavior
{
    /// <summary>
    /// Logs the persistence failure, detaches the uncommitted audit entity from the change tracker,
    /// and allows the request or caller to continue normally.
    /// Default setting to preserve backward compatibility for v1.0.x consumers.
    /// </summary>
    LogAndContinue = 0,

    /// <summary>
    /// Detaches the uncommitted audit entity from the change tracker and rethrows the persistence exception.
    /// Recommended for audit-critical, compliance-regulated, or financial applications.
    /// </summary>
    Throw = 1
}

