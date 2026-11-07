using OpenBible.Infrastructure.Sources;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class EbibleHtmlParserTests
{
    [Fact]
    public void Parse_IncludesOnlyRedistributableEntriesWithLanguage()
    {
        const string html = """
            <html><body>
            <table>
            <tr class='redist'>
            <td><a href='country.php?c=AL'><img src='../flags/al.png'/> Albania</a></td>
            <td><a href='details.php?id=rup' target='_blank' class='liberation_sans redist'>Armãneashti</a></td>
            <td><a href='details.php?id=rup' target='_blank' class='liberation_sans redist'>Aromanian</a></td>
            <td><a href='details.php?id=rup' target='_blank' class='liberation_sans redist'>Biblija tu limba Rrãmãnã</a></td>
            <td><a href='details.php?id=rup' target='_blank'>Aromanian Bible</a></td>
            </tr>
            <tr class='restricted'>
            <td><a href='country.php?c=US'><img src='../flags/us.png'/> United States</a></td>
            <td><a href='details.php?id=eng-web' target='_blank' class='liberation_sans restricted'>English</a></td>
            <td><a href='details.php?id=eng-web' target='_blank' class='liberation_sans restricted'>English</a></td>
            <td><a href='details.php?id=eng-web' target='_blank' class='liberation_sans restricted'>World English Bible</a></td>
            <td><a href='details.php?id=eng-web' target='_blank'>World English Bible</a></td>
            </tr>
            </table>
            </body></html>
            """;

        var items = EbibleHtmlParser.Parse(html, "https://ebible.org/Scriptures/$TRANSLATION_ID_usfm.zip");

        var item = Assert.Single(items);
        Assert.Equal("rup", item.Id);
        Assert.Equal("Aromanian", item.Language);
        Assert.Equal("Aromanian Bible", item.DisplayName);
        Assert.Equal("https://ebible.org/Scriptures/rup_usfm.zip", item.DownloadUrl);
    }

    [Fact]
    public void Parse_DeduplicatesRepeatedIdsAndKeepsTheLongerName()
    {
        const string html = """
            <table>
            <tr class='redist'>
            <td>Albania</td>
            <td><a href='details.php?id=eng-kjv' class='redist'>English</a></td>
            <td>English</td>
            <td>KJV</td>
            <td><a href='details.php?id=eng-kjv'>KJV</a></td>
            </tr>
            <tr class='redist'>
            <td>Albania</td>
            <td><a href='details.php?id=eng-kjv' class='redist'>English</a></td>
            <td>English</td>
            <td>King James Version</td>
            <td><a href='details.php?id=eng-kjv'>King James Version</a></td>
            </tr>
            </table>
            """;

        var item = Assert.Single(EbibleHtmlParser.Parse(html, "https://example.org/$TRANSLATION_ID_usfm.zip"));
        Assert.Equal("King James Version", item.DisplayName);
        Assert.Equal("English", item.Language);
    }
}
