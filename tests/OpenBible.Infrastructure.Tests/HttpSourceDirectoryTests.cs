using System.Net;
using System.Text;
using OpenBible.Domain.Enums;
using OpenBible.Infrastructure.Sources;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class HttpSourceDirectoryTests
{
    [Fact]
    public async Task GetSources_CallsRegistryAndMapsEntries()
    {
        var handler = Json(HttpStatusCode.OK, """
            [ { "id": "yes-example", "displayName": "YES example", "format": "yes", "catalogKind": "file" },
              { "id": "broken", "format": "unknown" } ]
            """);

        var sources = await Create(handler).GetSourcesAsync();

        var source = Assert.Single(sources);
        Assert.Equal("yes-example", source.Id);
        Assert.Equal("YES example", source.DisplayName);
        Assert.Equal(BibleFormat.Yes, source.Format);
        Assert.Equal(SourceCatalogKind.File, source.CatalogKind);
        Assert.Equal("https://app.test/api/sources", handler.LastRequest!.ToString());
    }

    [Fact]
    public async Task GetTranslations_ForwardsSearchAndMapsEntries()
    {
        var handler = Json(HttpStatusCode.OK, """
            [ { "id": "kjv", "displayName": "King James Version", "language": "en", "description": "Public domain", "license": "Public domain", "downloadUrl": "/api/sources/yes/download/kjv" } ]
            """);

        var translations = await Create(handler).GetTranslationsAsync("yes-example", "james");

        var translation = Assert.Single(translations);
        Assert.Equal("kjv", translation.Id);
        Assert.Equal("Public domain", translation.License);
        Assert.Equal("https://app.test/api/sources/yes-example/catalog?search=james", handler.LastRequest!.ToString());
    }

    [Fact]
    public async Task Download_ReturnsBufferedStream()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, [1, 2, 3]);

        await using var stream = await Create(handler).DownloadAsync("yes-example", "kjv");

        Assert.Equal("https://app.test/api/sources/yes-example/download/kjv", handler.LastRequest!.ToString());
        Assert.Equal(3, stream.Length);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task Download_WhenMissing_ExplainsManualImport()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.NotFound, []);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Create(handler).DownloadAsync("yes-example", "kjv"));

        Assert.Contains("manual import", ex.Message);
    }

    private static StubHttpMessageHandler Json(HttpStatusCode status, string body) =>
        new(status, Encoding.UTF8.GetBytes(body));

    private static HttpSourceDirectory Create(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://app.test/") });
}
