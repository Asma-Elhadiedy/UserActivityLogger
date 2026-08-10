
namespace UserActivityLogger.Constants;

public static class ConstLogMessage
{
    public const string LogUserActivityAttributeNotFound = "LogUserActivityAttribute not found on the action.";
    public const string HttpContextIsNull = "HttpContext is null, cannot log user activity.";
    public const string FailedToSaveUserActivityLog = "Failed to save user activity log";
    public const string UserActivityLoggingFilterExecuting = "Executing UserActivityLoggingFilter for action: {ActionName}";
    public const string UserActivityLoggingFilterExecuted = "Executed UserActivityLoggingFilter for action: {ActionName}";
}
