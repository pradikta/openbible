using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Infrastructure.Sword;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SwordFormatReaderTests
{
    private const string VerseOne = "<w lemma=\"x\">In the beginning</w> God created.";
    private const string VerseTwo = "And the earth was void.<note>footnote text</note>";

    [Fact]
    public async Task ReadMetadata_ReturnsKjvBooksAndLicense()
    {
        await using var module = BuildModule(ZTextConf(), BuildData(VerseOne, VerseTwo));

        var version = await new SwordFormatReader().ReadMetadataAsync(module);

        Assert.Equal(BibleFormat.Sword, version.Format);
        Assert.Equal("Test Module", version.Title);
        Assert.Equal("en", version.Language);
        Assert.Equal("Public domain", version.License);
        Assert.Equal(39, version.Books.Count);
        var genesis = version.Books[0];
        Assert.Equal(BookId.Genesis, genesis.BookId);
        Assert.Equal(50, genesis.ChapterCount);
        Assert.Equal(31, genesis.VerseCounts[0]);
    }

    [Fact]
    public async Task ReadVerses_CleansMarkupAndNotes()
    {
        await using var module = BuildModule(ZTextConf(), BuildData(VerseOne, VerseTwo));
        var verses = new List<Verse>();

        await foreach (var verse in new SwordFormatReader().ReadVersesAsync(module))
        {
            verses.Add(verse);
        }

        Assert.Equal(2, verses.Count);
        Assert.Equal(new Domain.ValueObjects.VerseReference(BookId.Genesis, 1, 1), verses[0].Reference);
        Assert.Equal("In the beginning God created.", verses[0].Text);
        Assert.Equal(new Domain.ValueObjects.VerseReference(BookId.Genesis, 1, 2), verses[1].Reference);
        Assert.Equal("And the earth was void.", verses[1].Text);
    }

    [Fact]
    public async Task ReadMetadata_ZText4_UsesTwelveByteRecords()
    {
        await using var module = BuildModule(ZTextConf(modDrv: "zText4"), BuildData(VerseOne, VerseTwo, ztext4: true));

        var version = await new SwordFormatReader().ReadMetadataAsync(module);

        Assert.Equal(BibleFormat.Sword, version.Format);
    }

    [Fact]
    public async Task ReadMetadata_UnsupportedVersification_Throws()
    {
        await using var module = BuildModule(ZTextConf(versification: "LXX"), BuildData(VerseOne, VerseTwo));

        await Assert.ThrowsAsync<NotSupportedException>(
            () => new SwordFormatReader().ReadMetadataAsync(module));
    }

    [Fact]
    public async Task ReadMetadata_UnsupportedCompression_Throws()
    {
        await using var module = BuildModule(ZTextConf(compressType: "BZIP2"), BuildData(VerseOne, VerseTwo));

        await Assert.ThrowsAsync<NotSupportedException>(
            () => new SwordFormatReader().ReadMetadataAsync(module));
    }

    [Fact]
    public async Task ReadMetadata_MissingConf_Throws()
    {
        await using var module = BuildModuleWithoutConf();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => new SwordFormatReader().ReadMetadataAsync(module));
    }

    [Fact]
    public async Task ReadPericopes_ReturnsEmpty()
    {
        await using var module = BuildModule(ZTextConf(), BuildData(VerseOne, VerseTwo));
        var count = 0;

        await foreach (var _ in new SwordFormatReader().ReadPericopesAsync(module))
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    private static string ZTextConf(
        string modDrv = "zText",
        string compressType = "ZIP",
        string versification = "KJV") =>
        $"""
        [Test]
        DataPath=modules/texts/ztext/test
        ModDrv={modDrv}
        BlockType=BOOK
        CompressType={compressType}
        Versification={versification}
        SourceType=OSIS
        Encoding=UTF-8
        Description=Test Module
        Lang=en
        DistributionLicense=Public domain
        """;

    private static (byte[] Bzv, byte[] Bzs, byte[] Bzz) BuildData(string verseOne, string verseTwo, bool ztext4 = false)
    {
        var buffer = Encoding.UTF8.GetBytes(verseOne + verseTwo);
        var compressed = Compress(buffer);
        var recordSize = ztext4 ? 12 : 10;
        var bzv = new byte[recordSize * 6];
        WriteVerseRecord(bzv, 4 * recordSize, verseOne.Length, ztext4);
        WriteVerseRecord(bzv, 5 * recordSize, verseTwo.Length, ztext4);

        var bzs = new byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(bzs.AsSpan(0), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bzs.AsSpan(4), (uint)compressed.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(bzs.AsSpan(8), (uint)buffer.Length);
        return (bzv, bzs, compressed);
    }

    private static void WriteVerseRecord(byte[] target, int offset, int length, bool ztext4)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(target.AsSpan(offset), 0);
        var start = offset == 4 * (ztext4 ? 12 : 10) ? 0 : VerseOne.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(target.AsSpan(offset + 4), (uint)start);
        if (ztext4)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(target.AsSpan(offset + 8), (uint)length);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(target.AsSpan(offset + 8), (ushort)length);
        }
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return output.ToArray();
    }

    private static MemoryStream BuildModule(string conf, (byte[] Bzv, byte[] Bzs, byte[] Bzz) data)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "mods.d/test.conf", Encoding.UTF8.GetBytes(conf));
            WriteEntry(archive, "modules/texts/ztext/test/ot.bzv", data.Bzv);
            WriteEntry(archive, "modules/texts/ztext/test/ot.bzs", data.Bzs);
            WriteEntry(archive, "modules/texts/ztext/test/ot.bzz", data.Bzz);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream BuildModuleWithoutConf()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "modules/texts/ztext/test/ot.bzv", [1, 2, 3]);
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] bytes)
    {
        var entry = archive.CreateEntry(name);
        using var stream = entry.Open();
        stream.Write(bytes);
    }
}
