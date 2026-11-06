using OpenBible.Infrastructure.Sources;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SourceCatalogParserTests
{
    [Fact]
    public void Parse_UsesUrlTemplateAndExplicitUrl()
    {
        const string json = """
            {
              "downloadUrlFormat": "https://example.org/files/$TRANSLATION_ID.yes",
              "items": [
                { "id": "kjv", "name": "King James Version", "language": "en", "description": "Public domain" },
                { "id": "tb", "name": "Terjemahan Baru", "language": "id", "url": "https://example.org/tb.yes", "license": "CC BY" }
              ]
            }
            """;

        var translations = SourceCatalogParser.Parse(json);

        Assert.Equal(2, translations.Count);
        Assert.Equal("https://example.org/files/kjv.yes", translations[0].DownloadUrl);
        Assert.Null(translations[0].License);
        Assert.Equal("https://example.org/tb.yes", translations[1].DownloadUrl);
        Assert.Equal("CC BY", translations[1].License);
    }

    [Fact]
    public void Parse_SkipsItemsWithoutId()
    {
        const string json = """
            { "items": [ { "name": "No id", "url": "https://example.org/x.yes" }, { "id": "ok", "url": "https://example.org/ok.yes" } ] }
            """;

        var translations = SourceCatalogParser.Parse(json);

        var single = Assert.Single(translations);
        Assert.Equal("ok", single.Id);
        Assert.Equal("ok", single.DisplayName);
    }

    [Fact]
    public void Parse_WithoutItems_ReturnsEmpty()
    {
        Assert.Empty(SourceCatalogParser.Parse("""{ "downloadUrlFormat": "https://example.org/$TRANSLATION_ID" }"""));
    }
}
