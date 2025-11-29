
namespace UserActivityLogger.Helpers;

/// 
/// Helper class for IP address operations.
/// 

internal static class IpAddressHelper
{
    /// 
    /// Converts an IPAddress to a string representation.
    /// Handles IPv6 to IPv4 mapping.
    /// 
    /// The IP address to convert.
    /// String representation of the IP address.
    public static string? GetIpAddressString(IPAddress? ipAddress)
    {
        if (ipAddress == null)
            return null;

        // Handle IPv6 to IPv4 mapping
        if (ipAddress.IsIPv4MappedToIPv6)
        {
            return ipAddress.MapToIPv4().ToString();
        }

        return ipAddress.ToString();
    }
}