using OpenBible.Infrastructure.Sources;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class AlkitabVersionConfigParserTests
{
    [Fact]
    public void Parse_MapsPresetsToItems()
    {
        const string json = """
            {
              "download_url_format": "https://alkitab-host.appspot.com/versions/get_yes?preset_name=$PRESET_NAME",
              "presets": [
                { "preset_name": "in-tb", "locale": "id", "shortName": "TB", "longName": "Terjemahan Baru", "description": "Indonesian" },
                { "preset_name": "ace-aceh", "locale": "ace", "shortName": "ACEH", "longName": "Aceh", "description": "" }
              ]
            }
            """;

        var items = AlkitabVersionConfigParser.Parse(json, "https://files.example.org/yes2/$TRANSLATION_ID--1.yes.gz");

        Assert.Equal(2, items.Count);
        Assert.Equal("in-tb", items[0].Id);
        Assert.Equal("Terjemahan Baru", items[0].DisplayName);
        Assert.Equal("id", items[0].Language);
        Assert.Equal("https://files.example.org/yes2/in-tb--1.yes.gz", items[0].DownloadUrl);
        Assert.Equal("https://files.example.org/yes2/ace-aceh--1.yes.gz", items[1].DownloadUrl);
    }

    [Fact]
    public void Parse_WithoutPresets_ReturnsEmpty()
    {
        Assert.Empty(AlkitabVersionConfigParser.Parse("{}", "https://example.org/$TRANSLATION_ID"));
    }
}
