

namespace UserActivityLogger.Filters;

/// 
/// Action filter for logging user activities based on LogUserActivityAttribute.
/// 
public class UserActivityLoggingFilter : IAsyncResultFilter
{
    private readonly ILogger<UserActivityLoggingFilter> _logger;
    private readonly IUserActivityLogger _userLogger;
    private readonly UserActivityLoggerOptions _options;
    public UserActivityLoggingFilter(ILogger<UserActivityLoggingFilter> logger, IUserActivityLogger userLogger, UserActivityLoggerOptions options)
    {
        _logger = logger;
        _userLogger = userLogger;
        _options = options;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<LogUserActivityAttribute>()
            .FirstOrDefault(); 

        if (attribute == null)
        {
            await next();
            return;
        }

        var httpContext = context.HttpContext;
        object? requestBody = null;

        // Check if we should log request body
        if ((attribute.LogRequestBody || _options.EnableRequestBodyLogging)
            && httpContext.Request.HasFormContentType)
        {
            requestBody = CaptureFormData(httpContext);
        }

        // Determine action description
        var action = attribute.ActionDescription
                     ?? context.ActionDescriptor.DisplayName
                     ?? "Unknown Action";

        // Execute the result
        var executedContext = await next();

        // Check for conditional action override
        if (attribute.LogActionArguments
            && executedContext.HttpContext.Items.ContainsKey("ConditionalLogAction"))
        {
            var conditionalAction = executedContext.HttpContext.Items["ConditionalLogAction"]?.ToString();
            if (!string.IsNullOrEmpty(conditionalAction))
            {
                action = conditionalAction;
            }
        }

        // Log the activity
        await _userLogger.LogActionAsync(action, requestBody);
    }

    private Dictionary<string, string>? CaptureFormData(HttpContext httpContext)
    {
        try
        {
            var formData = new Dictionary<string, string>();

            foreach (var key in httpContext.Request.Form.Keys)
            {
                // Skip anti-forgery tokens
                if (key.StartsWith("__"))
                    continue;

                // Skip DataTables parameters
                if (key.StartsWith("columns[") ||
                    key.StartsWith("order[") ||
                    key.StartsWith("search[") ||
                    key.Equals("draw", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("start", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("length", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if field should be redacted
                if (ShouldRedactField(key))
                {
                    formData[key] = _options.RedactionPlaceholder;
                }
                else
                {
                    var value = httpContext.Request.Form[key].ToString();

                    // Simplify boolean values
                    if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                        formData[key] = "True";
                    else if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
                        formData[key] = "False";
                    else
                        formData[key] = value;
                }
            }

            return formData.Any() ? formData : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture form data");
            return null;
        }
    }

    private bool ShouldRedactField(string fieldName)
    {
        return _options.RedactedFields.Any(redactedField =>
            fieldName.Contains(redactedField, StringComparison.OrdinalIgnoreCase));
    }

    //private LogUserActivityAttribute? GetLogUserActivityAttribute(ResultExecutingContext context)
    //{
    //    if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
    //    {
    //        // Check method level first
    //        var methodAttribute = controllerActionDescriptor.MethodInfo
    //            .GetCustomAttributes(typeof(LogUserActivityAttribute), inherit: true)
    //            .FirstOrDefault() as LogUserActivityAttribute;

    //        if (methodAttribute != null)
    //            return methodAttribute;

    //        // Fallback to controller level
    //        var controllerAttribute = controllerActionDescriptor.ControllerTypeInfo
    //            .GetCustomAttributes(typeof(LogUserActivityAttribute), inherit: true)
    //            .FirstOrDefault() as LogUserActivityAttribute;

    //        return controllerAttribute;
    //    }

    //    return null;
    //}
}