
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

        // 1. Handle the IPv6 loopback address (::1)
        if (IPAddress.IPv6Loopback.Equals(ipAddress))
            return IPAddress.Loopback.ToString(); 
        

        // 2. Handle IPv4 to IPv6 mapped addresses (::FFFF:<IPv4>)
        if (ipAddress.IsIPv4MappedToIPv6)
            return ipAddress.MapToIPv4().ToString();
        

        return ipAddress.ToString();
    }
}