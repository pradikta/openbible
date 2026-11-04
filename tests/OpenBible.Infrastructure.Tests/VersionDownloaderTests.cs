using System.Net;
using OpenBible.Infrastructure.Catalog;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class VersionDownloaderTests
{
    private sealed class StubHandler(HttpStatusCode status, byte[] body) : HttpMessageHandler
    {
        public Uri? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(body) });
        }
    }

    private static HttpVersionDownloader Create(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://app.test/") });

    [Fact]
    public async Task Download_CallsServerEndpointAndBuffersBody()
    {
        var handler = new StubHandler(HttpStatusCode.OK, [1, 2, 3]);

        await using var stream = await Create(handler).DownloadAsync("in-tb");

        Assert.Equal("https://app.test/api/catalog/download/in-tb", handler.LastRequest!.ToString());
        Assert.Equal(3, stream.Length);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task Download_WhenEndpointMissing_ExplainsFallback()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Create(new StubHandler(HttpStatusCode.NotFound, [])).DownloadAsync("x"));

        Assert.Contains("Manual download", ex.Message);
    }

    [Fact]
    public async Task Download_OnServerError_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Create(new StubHandler(HttpStatusCode.BadGateway, [])).DownloadAsync("x"));
    }
}
