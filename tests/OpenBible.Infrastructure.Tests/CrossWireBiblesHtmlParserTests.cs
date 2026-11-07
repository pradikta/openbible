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
            <br id="lang_en" /><br /><b>Language: en</b><br>
            <table class="moddisp">
            <thead><tr><th>Info</th><th>Name</th><th>Description</th><th>Raw ZIP</th></tr></thead>
            <tbody>
            <tr>
            <td><a href="ModInfo.jsp?modName=KJV">[info]</a></td>
            <td>KJV</td>
            <td>King James Version</td>
            <td><a href="/sword/servlet/SwordMod.Verify?modName=KJV&pkgType=raw">[download]</a></td>
            </tr>
            <tr>
            <td><a href="ModInfo.jsp?modName=ASV">[info]</a></td>
            <td>ASV</td>
            <td>American Standard Version</td>
            <td><a href="/sword/servlet/SwordMod.Verify?modName=ASV&pkgType=raw">[download]</a></td>
            </tr>
            </tbody></table>
            </body></html>
            """;

        var items = CrossWireBiblesHtmlParser.Parse(html, "https://crosswire.org/files/$TRANSLATION_ID.zip");

        Assert.Equal(2, items.Count);
        Assert.Contains(items, item =>
            item.Id == "KJV"
            && item.Language == "English"
            && item.DownloadUrl == "https://crosswire.org/files/KJV.zip");
        Assert.Contains(items, item => item.Id == "ASV" && item.DownloadUrl == "https://crosswire.org/files/ASV.zip");
    }

    [Fact]
    public void Parse_ReadsLanguageSectionAndDescription()
    {
        const string html = """
            <html><body>
            <br id="lang_af" /><br /><b>Language: af</b><br>
            <table class="moddisp">
            <thead><tr><th>Info</th><th>Name</th><th>Description</th><th>Raw ZIP</th></tr></thead>
            <tbody>
            <tr>
            <td><a href="ModInfo.jsp?modName=Afr1953">[info]</a></td>
            <td>Afr1953</td>
            <td>1933/1953 Afrikaans Bybel</td>
            <td><a href="/sword/servlet/SwordMod.Verify?modName=Afr1953&pkgType=raw">[download]</a></td>
            </tr>
            </tbody></table>
            <br id="lang_ar" /><br /><b>Language: ar</b><br>
            <table class="moddisp">
            <thead><tr><th>Info</th><th>Name</th><th>Description</th><th>Raw ZIP</th></tr></thead>
            <tbody>
            <tr>
            <td><a href="ModInfo.jsp?modName=AraSVD">[info]</a></td>
            <td>AraSVD</td>
            <td>Smith and van Dyck's al-Kitab al-Muqaddas (Arabic Bible)</td>
            <td><a href="/sword/servlet/SwordMod.Verify?modName=AraSVD&pkgType=raw">[download]</a></td>
            </tr>
            </tbody></table>
            </body></html>
            """;

        var items = CrossWireBiblesHtmlParser.Parse(html, "https://crosswire.org/files/$TRANSLATION_ID.zip");

        Assert.Equal(2, items.Count);
        var afrikaans = Assert.Single(items, item => item.Id == "Afr1953");
        Assert.Equal("Afrikaans", afrikaans.Language);
        Assert.Equal("1933/1953 Afrikaans Bybel", afrikaans.Description);
        Assert.Equal("https://crosswire.org/files/Afr1953.zip", afrikaans.DownloadUrl);

        var arabic = Assert.Single(items, item => item.Id == "AraSVD");
        Assert.Equal("Arabic", arabic.Language);
        Assert.Equal("Smith and van Dyck's al-Kitab al-Muqaddas (Arabic Bible)", arabic.Description);
    }
}
