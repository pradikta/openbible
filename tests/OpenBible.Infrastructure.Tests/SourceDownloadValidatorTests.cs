using System.Net;
using System.Net.Sockets;
using OpenBible.Infrastructure.Sources;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SourceDownloadValidatorTests
{
    private static readonly IPAddress PublicAddress = IPAddress.Parse("93.184.216.34");

    [Fact]
    public async Task IsAllowedAsync_PublicHttpsHost_ReturnsTrue()
    {
        var validator = Create(_ => [PublicAddress]);

        Assert.True(await validator.IsAllowedAsync(new Uri("https://example.org/file.yes"), "example.org"));
    }

    [Theory]
    [InlineData("http://example.org/file.yes")]
    [InlineData("file:///etc/passwd")]
    public async Task IsAllowedAsync_NonHttps_ReturnsFalse(string url)
    {
        var validator = Create(_ => [PublicAddress]);

        Assert.False(await validator.IsAllowedAsync(new Uri(url), "example.org"));
    }

    [Fact]
    public async Task IsAllowedAsync_HostMismatch_ReturnsFalse()
    {
        var validator = Create(_ => [PublicAddress]);

        Assert.False(await validator.IsAllowedAsync(new Uri("https://evil.example/file.yes"), "example.org"));
    }

    [Theory]
    [InlineData("https://127.0.0.1/file.yes")]
    [InlineData("https://10.0.0.5/file.yes")]
    [InlineData("https://192.168.1.1/file.yes")]
    [InlineData("https://169.254.1.1/file.yes")]
    public async Task IsAllowedAsync_PrivateLiteralAddress_ReturnsFalse(string url)
    {
        var validator = Create(_ => [PublicAddress]);
        var uri = new Uri(url);

        Assert.False(await validator.IsAllowedAsync(uri, uri.Host));
    }

    [Fact]
    public async Task IsAllowedAsync_PrivateResolvedAddress_ReturnsFalse()
    {
        var validator = Create(_ => [IPAddress.Parse("10.1.2.3")]);

        Assert.False(await validator.IsAllowedAsync(new Uri("https://internal.example/file.yes"), "internal.example"));
    }

    [Fact]
    public async Task IsAllowedAsync_ResolverFailure_ReturnsFalse()
    {
        var validator = Create(_ => throw new SocketException());

        Assert.False(await validator.IsAllowedAsync(new Uri("https://missing.example/file.yes"), "missing.example"));
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.32.0.1", false)]
    [InlineData("192.168.0.1", true)]
    [InlineData("8.8.8.8", false)]
    [InlineData("::1", true)]
    [InlineData("fe80::1", true)]
    [InlineData("fc00::1", true)]
    [InlineData("2001:4860:4860::8888", false)]
    public void IsPrivateOrLocal_ClassifiesAddresses(string value, bool expected)
    {
        Assert.Equal(expected, SourceDownloadValidator.IsPrivateOrLocal(IPAddress.Parse(value)));
    }

    private static SourceDownloadValidator Create(Func<string, IPAddress[]> resolve) =>
        new((host, _) => Task.FromResult(resolve(host)));
}
