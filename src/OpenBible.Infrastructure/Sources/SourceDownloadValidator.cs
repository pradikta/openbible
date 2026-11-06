using System.Net;
using System.Net.Sockets;

namespace OpenBible.Infrastructure.Sources;

/// <summary>
/// Enforces HTTPS, an exact host match, and public addresses (including after DNS resolution) so
/// the download proxy cannot be used for SSRF.
/// </summary>
public sealed class SourceDownloadValidator
{
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _resolve;

    public SourceDownloadValidator(Func<string, CancellationToken, Task<IPAddress[]>>? resolver = null)
    {
        _resolve = resolver ?? ((host, ct) => Dns.GetHostAddressesAsync(host, ct));
    }

    public async Task<bool> IsAllowedAsync(Uri uri, string allowedHost, CancellationToken ct = default)
    {
        if (!uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (!string.Equals(uri.Host, allowedHost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out var literal) && IsPrivateOrLocal(literal))
        {
            return false;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await _resolve(uri.Host, ct).ConfigureAwait(false);
        }
        catch (SocketException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(address => !IsPrivateOrLocal(address));
    }

    public static bool IsPrivateOrLocal(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
        {
            return true;
        }

        var ipv4 = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        if (ipv4.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = ipv4.GetAddressBytes();
        return bytes[0] == 10
            || bytes[0] == 127
            || bytes[0] == 0
            || (bytes[0] == 169 && bytes[1] == 254)
            || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 100 && bytes[1] is >= 64 and <= 127);
    }
}
