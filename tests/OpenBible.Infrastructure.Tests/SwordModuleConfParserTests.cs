using OpenBible.Infrastructure.Sword;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SwordModuleConfParserTests
{
    [Fact]
    public void Parse_ReadsCoreFieldsAndContinuationLines()
    {
        const string conf = """
            # comment
            [KJV]
            DataPath=modules/texts/ztext/kjv
            ModDrv=zText
            BlockType=BOOK
            CompressType=ZIP
            Versification=KJV
            SourceType=OSIS
            Encoding=UTF-8
            Description=King James Version
            Lang=en
            DistributionLicense=Public domain
            Copyright=Line one
             line two
            """;

        var parsed = SwordModuleConfParser.Parse(conf, "fallback");

        Assert.Equal("KJV", parsed.Name);
        Assert.Equal("modules/texts/ztext/kjv", parsed.DataPath);
        Assert.Equal("zText", parsed.ModDrv);
        Assert.Equal("BOOK", parsed.BlockType);
        Assert.Equal("ZIP", parsed.CompressType);
        Assert.Equal("KJV", parsed.Versification);
        Assert.Equal("UTF-8", parsed.Encoding);
        Assert.Equal("King James Version", parsed.Title);
        Assert.Equal("en", parsed.Language);
        Assert.Equal("Public domain", parsed.License);
    }

    [Fact]
    public void Parse_WithoutDataPath_Throws()
    {
        Assert.Throws<InvalidDataException>(() => SwordModuleConfParser.Parse("[Test]\nDescription=x", "test"));
    }

    [Fact]
    public void Parse_FallsBackToAboutAndCopyright()
    {
        const string conf = "DataPath=modules\nAbout=About text\nCopyright=Copyright text";

        var parsed = SwordModuleConfParser.Parse(conf, "fallback");

        Assert.Equal("About text", parsed.Title);
        Assert.Equal("Copyright text", parsed.License);
    }
}
