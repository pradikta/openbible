using System.Buffers.Binary;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Infrastructure.YesFile;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class YesFilePericopeTests
{
    private readonly YesFileParser _parser = new();

    [Fact]
    public async Task ReadPericopesAsync_Version3Index_ReadsAbsoluteAndDeltaEntries()
    {
        using var stream = Yes2TestFile.Create(("pericopes", CreatePericopeSection()));

        var pericopes = await ReadAllAsync(stream);

        Assert.Equal(2, pericopes.Count);

        var first = pericopes[0];
        Assert.Equal(BookId.Genesis, first.Reference.Book);
        Assert.Equal(1, first.Reference.Chapter);
        Assert.Equal(1, first.Reference.Verse);
        Assert.Equal("The Creation", first.Title);
        Assert.Equal("John 1:1", Assert.Single(first.ParallelPassages));

        var second = pericopes[1];
        Assert.Equal(1, second.Reference.Chapter);
        Assert.Equal(6, second.Reference.Verse);
        Assert.Equal("The Fall", second.Title);
        Assert.Empty(second.ParallelPassages);
    }

    [Fact]
    public async Task ReadPericopesAsync_SnappyBlocksCompressedSection_ReadsPericopes()
    {
        var payload = CreatePericopeSection();
        var block = Yes2TestFile.SnappyLiteral(payload);
        var attributes = Yes2TestFile.SnappyBlocksAttributes(payload.Length, block.Length);
        using var stream = Yes2TestFile.CreateWithAttributes(("pericopes", attributes, block));

        var pericopes = await ReadAllAsync(stream);

        Assert.Equal(2, pericopes.Count);
        Assert.Equal("The Creation", pericopes[0].Title);
    }

    [Fact]
    public async Task ReadPericopesAsync_MissingSection_ReturnsEmpty()
    {
        using var stream = Yes2TestFile.Create(("text", [0x00]));

        var pericopes = await ReadAllAsync(stream);

        Assert.Empty(pericopes);
    }

    [Fact]
    public async Task ReadPericopesAsync_UnsupportedDataFormatVersion_ThrowsNotSupportedException()
    {
        using var stream = Yes2TestFile.Create(("pericopes", [0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]));

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await foreach (var _ in _parser.ReadPericopesAsync(stream))
            {
            }
        });
    }

    [Fact]
    public async Task SaveVersionAsync_StoresParsedPericopesForChapterLookup()
    {
        using var stream = Yes2TestFile.Create(
            ("versionInfo", Yes2TestFile.MetadataMap()),
            ("booksInfo", Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", [1, (byte)'A']),
            ("pericopes", CreatePericopeSection()));
        var metadata = await _parser.ReadMetadataAsync(stream);
        var repository = new OpenBible.Infrastructure.Persistence.InMemoryBibleRepository();

        await repository.SaveVersionAsync(
            metadata,
            _parser.ReadVersesAsync(stream),
            _parser.ReadPericopesAsync(stream));

        var pericopes = await repository.GetChapterPericopesAsync(metadata.Id, BookId.Genesis, 1);

        Assert.Equal(2, pericopes.Count);
        Assert.Equal("The Creation", pericopes[0].Title);
        Assert.Equal(6, pericopes[1].Reference.Verse);
    }

    private async Task<List<Pericope>> ReadAllAsync(Stream stream)
    {
        var pericopes = new List<Pericope>();
        await foreach (var pericope in _parser.ReadPericopesAsync(stream))
        {
            pericopes.Add(pericope);
        }

        return pericopes;
    }

    private static byte[] CreatePericopeSection()
    {
        var firstBlock = CreatePericopeBlock("The Creation", ["John 1:1"]);
        var secondBlock = CreatePericopeBlock("The Fall", []);
        var untitledBlock = CreatePericopeBlock(string.Empty, []);

        using var stream = new MemoryStream();
        stream.WriteByte(3); // data format version
        Yes2TestFile.WriteInt32(stream, 0); // Index size, informational only.
        Yes2TestFile.WriteInt32(stream, 3); // entry count

        WriteAbsoluteAri(stream, 0x000101); // Genesis 1:1
        WriteUInt16(stream, 0);
        WriteAriDelta(stream, 5); // Genesis 1:6
        WriteUInt16(stream, (ushort)firstBlock.Length);
        WriteAriDelta(stream, 1); // Genesis 1:7, heading without a title.
        WriteUInt16(stream, (ushort)secondBlock.Length);

        stream.Write(firstBlock);
        stream.Write(secondBlock);
        stream.Write(untitledBlock);
        return stream.ToArray();
    }

    private static byte[] CreatePericopeBlock(string title, string[] parallelPassages)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(4); // block version
        stream.Write(Yes2TestFile.EncodeString(title));
        stream.WriteByte(checked((byte)parallelPassages.Length));
        foreach (var parallel in parallelPassages)
        {
            stream.Write(Yes2TestFile.EncodeString(parallel));
        }

        return stream.ToArray();
    }

    private static void WriteAbsoluteAri(Stream stream, int ari)
    {
        WriteUInt16(stream, (ushort)(ari >> 16));
        WriteUInt16(stream, (ushort)(ari & 0xffff));
    }

    private static void WriteAriDelta(Stream stream, int delta) => WriteUInt16(stream, (ushort)(0x8000 | delta));

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        stream.Write(bytes);
    }
}
