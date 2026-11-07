using OpenBible.Infrastructure.Sources;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class CrossWireBiblesHtmlParserTests
{
    [Fact]
    public void Parse_ExtractsUniqueModuleNames()
    {
        const string html = """
            <html><body>
            <a href='ModInfo.jsp?modName=KJV'>KJV</a>
            <a href='ModInfo.jsp?modName=KJV'>King James Version</a>
            <a href='ModInfo.jsp?modName=ASV'>ASV</a>
            </body></html>
            """;

        var items = CrossWireBiblesHtmlParser.Parse(html, "https://crosswire.org/files/$TRANSLATION_ID.zip");

        Assert.Equal(2, items.Count);
        Assert.Contains(items, item => item.Id == "KJV" && item.DownloadUrl == "https://crosswire.org/files/KJV.zip");
        Assert.Contains(items, item => item.Id == "ASV" && item.DownloadUrl == "https://crosswire.org/files/ASV.zip");
    }
}
