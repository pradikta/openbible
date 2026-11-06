using System.IO.Compression;
using System.Text;
using NSubstitute;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.YesFile;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class BibleFormatReaderSelectorTests
{
    [Fact]
    public void GetReader_RegisteredFormat_ReturnsReader()
    {
        var reader = Substitute.For<IBibleFormatReader>();
        reader.Format.Returns(BibleFormat.Yes);
        var selector = new BibleFormatReaderSelector([reader]);

        Assert.Same(reader, selector.GetReader(BibleFormat.Yes));
    }

    [Fact]
    public void GetReader_UnregisteredFormat_Throws()
    {
        var reader = Substitute.For<IBibleFormatReader>();
        reader.Format.Returns(BibleFormat.Yes);
        var selector = new BibleFormatReaderSelector([reader]);

        Assert.Throws<NotSupportedException>(() => selector.GetReader(BibleFormat.Usfm));
    }

    [Fact]
    public void DetectFormat_YesHeader_ReturnsYes()
    {
        using var stream = new MemoryStream([0x98, 0x58, 0x0d, 0x0a, 0x00, 0x5d, 0xe0, 0x02]);

        Assert.Equal(BibleFormat.Yes, Selector().DetectFormat(stream));
    }

    [Fact]
    public void DetectFormat_GzipMagic_ReturnsYes()
    {
        using var stream = new MemoryStream([0x1F, 0x8B, 0x08, 0x00]);

        Assert.Equal(BibleFormat.Yes, Selector().DetectFormat(stream));
    }

    [Fact]
    public void DetectFormat_UsfmText_ReturnsUsfm()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("\\id GEN\n\\c 1\n\\v 1 In the beginning."));

        Assert.Equal(BibleFormat.Usfm, Selector().DetectFormat(stream));
    }

    [Fact]
    public void DetectFormat_ZipWithUsfm_ReturnsUsfm()
    {
        using var stream = Zip(("GEN.usfm", "\\id GEN\n\\c 1\n\\v 1 text"));

        Assert.Equal(BibleFormat.Usfm, Selector().DetectFormat(stream));
    }

    [Fact]
    public void DetectFormat_ZipWithSwordConf_ReturnsSword()
    {
        using var stream = Zip(("mods.d/kjv.conf", "[KJV]\nModDrv=zText"));

        Assert.Equal(BibleFormat.Sword, Selector().DetectFormat(stream));
    }

    [Fact]
    public void DetectFormat_Unknown_ReturnsNull()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not a bible file"));

        Assert.Null(Selector().DetectFormat(stream));
    }

    private static BibleFormatReaderSelector Selector() => new([]);

    private static MemoryStream Zip(params (string Name, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
