using OpenBible.Infrastructure.Sources;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class EbibleHtmlParserTests
{
    [Fact]
    public void Parse_IncludesOnlyRedistributableEntries()
    {
        const string html = """
            <html><body>
            <a href='details.php?id=eng-kjv' target='_blank' class='liberation_sans redist'>King James Version</a>
            <a href='details.php?id=eng-kjv' target='_blank'>KJV</a>
            <a href='details.php?id=eng-web' target='_blank' class='liberation_sans restricted'>World English Bible</a>
            </body></html>
            """;

        var items = EbibleHtmlParser.Parse(html, "https://ebible.org/Scriptures/$TRANSLATION_ID_usfm.zip");

        var item = Assert.Single(items);
        Assert.Equal("eng-kjv", item.Id);
        Assert.Equal("King James Version", item.DisplayName);
        Assert.Equal("https://ebible.org/Scriptures/eng-kjv_usfm.zip", item.DownloadUrl);
    }

    [Fact]
    public void Parse_DeduplicatesRepeatedIds()
    {
        const string html = """
            <a href='details.php?id=eng-kjv' class='redist'>KJV</a>
            <a href='details.php?id=eng-kjv' class='redist'>King James Version</a>
            """;

        var item = Assert.Single(EbibleHtmlParser.Parse(html, "https://example.org/$TRANSLATION_ID_usfm.zip"));
        Assert.Equal("King James Version", item.DisplayName);
    }
}
