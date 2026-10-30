
namespace UserActivityLogger.Options;

/// <summary>
/// Configuration options for UserActivityLogger.
/// </summary>

public class UserActivityLoggerOptions
{
    /// 
    /// Gets or sets whether request body logging is enabled globally.
    /// Can be overridden per action using LogUserActivityAttribute.
    /// Default: false
    /// 
    public bool EnableRequestBodyLogging { get; set; } = false;

    /// 
    /// Gets or sets the list of form field names to redact (case-insensitive).
    /// Default: password, pwd, secret, token, apikey, api_key
    /// 
    public string[] RedactedFields { get; set; } = new[]
    {
        "password",
        "pwd",
        "secret",
        "token",
        "apikey",
        "api_key",
        "authorization"
    };

    /// 
    /// Gets or sets the list of paths to skip logging.
    /// Default: /health, /metrics
    /// 
    public string[] SkipPaths { get; set; } = new[]
    {
        "/health",
        "/metrics"
    };

    /// 
    /// Gets or sets the claim type used to retrieve user ID.
    /// Default: ClaimTypes.NameIdentifier
    /// 
    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

    /// 
    /// Gets or sets whether to log activities for anonymous users.
    /// Default: true
    /// 
    public bool LogAnonymousUsers { get; set; } = true;

    /// 
    /// Gets or sets a custom function to resolve IP addresses.
    /// Useful for scenarios with proxies or load balancers.
    /// 
    public Func<HttpContext, string>? IpAddressResolver { get; set; }

    /// 
    /// Gets or sets the redaction placeholder text.
    /// Default: ***REDACTED***
    /// 
    public string RedactionPlaceholder { get; set; } = "***REDACTED***";

    /// <summary>
    /// Gets or sets whether audit persistence is isolated from the application's scoped DbContext unit of work.
    /// When true (default), the service writes to an isolated DbContext instance (via IDbContextFactory or an isolated scope),
    /// guaranteeing that pending business entity changes in the scoped context are never committed by an audit write.
    /// When false, the service writes through the application's shared scoped context.
    /// </summary>
    public bool IsolatePersistence { get; set; } = true;
}