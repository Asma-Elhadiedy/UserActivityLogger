
namespace UserActivityLogger.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LogUserActivityAttribute : Attribute
{
    public string? ActionDescription { get; set; }

    public bool LogRequestBody { get; set; } = false;

    /// 
    /// Gets or sets whether to enable conditional action logging.
    /// When true, the action description can be overridden via HttpContext.Items["ConditionalLogAction"].
    /// Default: false
    /// 
    public bool LogActionArguments { get; set; } = false;

    public LogUserActivityAttribute(string? actionDescription)
    {
        ActionDescription = actionDescription;
    }
}