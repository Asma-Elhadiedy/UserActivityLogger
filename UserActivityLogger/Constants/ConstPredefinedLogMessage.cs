
namespace UserActivityLogger.Constants;

public static class ConstPredefinedLogMessage
{

    /// <summary>
    /// Takes Action Name as visited page name
    /// </summary>
    public const string UserVisitedPage = "User Visited {0} page in {1}.";


    /// <summary>
    /// Takes Controller Name as created resource type name
    /// </summary>
    public const string UserCreatedResource = "User created a resource of type {1}.";

    
    /// <summary>
    /// Takes Controller Name as updated resource type name
    /// </summary>
    public const string UserUpdatedResource = "User updated a resource of type {1}.";


    /// <summary>
    /// Takes Controller Name as deleted resource type name
    /// </summary>
    public const string UserDeletedResource = "User deleted a resource of type {1}.";

}
