using OpenBible.Domain.Exceptions;
using OpenBible.Infrastructure.YesFile;
using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class YesFileParserTests
{
    private readonly YesFileParser _parser = new();

    [Fact]
    public async Task ReadMetadataAsync_InvalidMagicHeader_ShouldThrowYesFileCorruptException()
    {
        var invalidBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(invalidBytes);

        await Assert.ThrowsAsync<YesFileCorruptException>(
            () => _parser.ReadMetadataAsync(stream)
        );
    }

    [Fact]
    public async Task ReadMetadataAsync_ValidVersionInfoSection_ShouldReadMetadata()
    {
        var metadata = CreateBintexMap(("shortName", "KJV"), ("longName", "King James Version"),
            ("locale", "en"), ("description", "English translation"));
        using var stream = CreateYes2File(("versionInfo", metadata), ("booksInfo", [0, 0, 0, 0]));

        var version = await _parser.ReadMetadataAsync(stream);

        Assert.Equal("kjv", version.Id);
        Assert.Equal("King James Version", version.Title);
        Assert.Equal("en", version.Language);
        Assert.Equal("English translation", version.Description);
    }

    [Fact]
    public void Yes2SectionIndex_ReadsSectionContentBoundary()
    {
        using var stream = CreateYes2File("text", [0x7f]);

        var index = Yes2SectionIndex.Read(stream);

        Assert.True(index.TryGetSection("text", out var section));
        Assert.NotNull(section);
        Assert.Equal(index.DataStartOffset, section.StartOffset);
        Assert.Equal(section.StartOffset, section.ContentOffset);
        Assert.Equal(1, section.ContentSize);
        stream.Position = section.ContentOffset;
        Assert.Equal(0x7f, stream.ReadByte());
    }

    [Fact]
    public void Yes2SectionIndex_RejectsSectionOutsideFile()
    {
        using var stream = CreateYes2File("text", [], declaredContentSize: 4);

        Assert.Throws<YesFileCorruptException>(() => Yes2SectionIndex.Read(stream));
    }

    [Fact]
    public async Task ReadVersesAsync_UsesBookChapterOffsetsAndLengthPrefixedText()
    {
        var metadata = CreateBintexMap(("shortName", "TST"), ("longName", "Test"), ("locale", "en"));
        var books = CreateBooksInfo();
        using var stream = CreateYes2File(("versionInfo", metadata), ("booksInfo", books), ("text", [1, (byte)'A']));
        var verses = new List<OpenBible.Domain.Entities.Verse>();

        await foreach (var verse in _parser.ReadVersesAsync(stream))
        {
            verses.Add(verse);
        }

        var verseResult = Assert.Single(verses);
        Assert.Equal("A", verseResult.Text);
        Assert.Equal(0, (int)verseResult.Reference.Book);
        Assert.Equal(1, verseResult.Reference.Chapter);
        Assert.Equal(1, verseResult.Reference.Verse);
    }

    private static MemoryStream CreateYes2File(string sectionName, byte[] content, int? declaredContentSize = null)
    {
        if (declaredContentSize is not null)
        {
            using var body = new MemoryStream();
            body.WriteByte(1);
            WriteInt32(body, 1);
            var name = Encoding.ASCII.GetBytes(sectionName);
            body.WriteByte((byte)name.Length);
            body.Write(name);
            WriteInt32(body, 0);
            WriteInt32(body, 0);
            WriteInt32(body, declaredContentSize.Value);
            body.Write([0, 0, 0, 0]);
            var invalidFile = new MemoryStream();
            invalidFile.Write(YesFileConstants.Yes2Header);
            WriteInt32(invalidFile, checked((int)body.Length));
            body.Position = 0;
            body.CopyTo(invalidFile);
            invalidFile.Write(content);
            invalidFile.Position = 0;
            return invalidFile;
        }

        return CreateYes2File((sectionName, content));
    }

    private static MemoryStream CreateYes2File(params (string Name, byte[] Content)[] sections)
    {
        using var indexBody = new MemoryStream();
        indexBody.WriteByte(1);
        WriteInt32(indexBody, sections.Length);
        var offset = 0;
        foreach (var (name, content) in sections)
        {
            var nameBytes = Encoding.ASCII.GetBytes(name);
            indexBody.WriteByte((byte)nameBytes.Length);
            indexBody.Write(nameBytes);
            WriteInt32(indexBody, offset);
            WriteInt32(indexBody, 0);
            WriteInt32(indexBody, content.Length);
            indexBody.Write([0, 0, 0, 0]);
            offset += content.Length;
        }

        var file = new MemoryStream();
        file.Write(YesFileConstants.Yes2Header);
        WriteInt32(file, checked((int)indexBody.Length));
        indexBody.Position = 0;
        indexBody.CopyTo(file);
        foreach (var (_, content) in sections)
        {
            file.Write(content);
        }
        file.WriteByte(0); // Documented footer.
        file.Position = 0;
        return file;
    }

    private static byte[] CreateBintexMap(params (string Key, string Value)[] entries)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(0x91);
        stream.WriteByte((byte)entries.Length);
        foreach (var (key, value) in entries)
        {
            var keyBytes = Encoding.ASCII.GetBytes(key);
            var valueBytes = Encoding.Latin1.GetBytes(value);
            stream.WriteByte((byte)keyBytes.Length);
            stream.Write(keyBytes);
            if (valueBytes.Length <= 15)
            {
                stream.WriteByte(checked((byte)(0x50 + valueBytes.Length)));
            }
            else
            {
                stream.WriteByte(0x70);
                stream.WriteByte(checked((byte)valueBytes.Length));
            }
            stream.Write(valueBytes);
        }

        return stream.ToArray();
    }

    private static byte[] CreateBooksInfo()
    {
        using var stream = new MemoryStream();
        WriteInt32(stream, 1);
        stream.WriteByte(0x91);
        stream.WriteByte(6);
        WriteBintexKey(stream, "bookId");
        stream.WriteByte(0x0e); // zero
        WriteBintexKey(stream, "shortName");
        stream.WriteByte(0x53);
        stream.Write(Encoding.Latin1.GetBytes("Gen"));
        WriteBintexKey(stream, "offset");
        stream.WriteByte(0x0e); // zero
        WriteBintexKey(stream, "chapter_count");
        stream.WriteByte(0x01);
        WriteBintexKey(stream, "verse_counts");
        stream.Write([0xc0, 1, 1]);
        WriteBintexKey(stream, "chapter_offsets");
        stream.Write([0xc0, 1, 0]);
        return stream.ToArray();
    }

    private static void WriteBintexKey(Stream stream, string key)
    {
        var bytes = Encoding.ASCII.GetBytes(key);
        stream.WriteByte((byte)bytes.Length);
        stream.Write(bytes);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }
}
