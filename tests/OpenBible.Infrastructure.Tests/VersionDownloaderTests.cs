using System.Net;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Catalog;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
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

    private static async Task<IAppSettingsRepository> SettingsAsync(string? proxy)
    {
        var settings = new PersistentAppSettingsRepository(new InMemoryPersistentStore());
        await settings.SetDownloadProxyUrlAsync(proxy);
        return settings;
    }

    [Fact]
    public async Task Download_WithoutProxy_Throws()
    {
        var downloader = new HttpVersionDownloader(new HttpClient(new StubHandler(HttpStatusCode.OK, [])), await SettingsAsync(null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync("https://host/a"));
    }

    [Fact]
    public async Task Download_UsesEscapedUrlInProxyTemplate()
    {
        var handler = new StubHandler(HttpStatusCode.OK, [1, 2, 3]);
        var downloader = new HttpVersionDownloader(new HttpClient(handler), await SettingsAsync("https://proxy.test/?url={url}"));

        await using var stream = await downloader.DownloadAsync("https://host/a?x=1");

        Assert.Equal("https://proxy.test/?url=https%3A%2F%2Fhost%2Fa%3Fx%3D1", handler.LastRequest!.ToString());
        Assert.Equal(3, stream.Length);
    }

    [Fact]
    public async Task Download_OnHttpError_Throws()
    {
        var downloader = new HttpVersionDownloader(
            new HttpClient(new StubHandler(HttpStatusCode.Forbidden, [])),
            await SettingsAsync("https://proxy.test/?url={url}"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync("https://host/a"));
    }

    [Fact]
    public async Task Settings_KeepProxyAndDefaultTogether()
    {
        var store = new InMemoryPersistentStore();
        var first = new PersistentAppSettingsRepository(store);
        await first.SetDownloadProxyUrlAsync("https://p/?url={url}");
        await first.SetDefaultVersionIdAsync("kjv");

        var second = new PersistentAppSettingsRepository(store);
        await second.RestoreAsync();

        Assert.Equal("kjv", await second.GetDefaultVersionIdAsync());
        Assert.Equal("https://p/?url={url}", await second.GetDownloadProxyUrlAsync());
    }
}
