using System.Text;
using OpenBible.Domain.Exceptions;
using OpenBible.Infrastructure.YesFile;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class YesFileSnappyTextTests
{
    private readonly YesFileParser _parser = new();

    [Fact]
    public async Task ReadVersesAsync_SnappyBlocksCompressedText_DecompressesVerses()
    {
        // Uncompressed text payload: VarUInt length 6 followed by "In the".
        var payload = Encoding.ASCII.GetBytes("\u0006In the");
        using var stream = CreateCompressedFixture(payload, blockSize: 32);

        var verses = await ReadAllVersesAsync(stream);

        var verse = Assert.Single(verses);
        Assert.Equal("In the", verse.Text);
        Assert.Equal(1, verse.Reference.Chapter);
        Assert.Equal(1, verse.Reference.Verse);
    }

    [Fact]
    public async Task ReadVersesAsync_SnappyBlocksSplittingPayloadAcrossBlocks_DecompressesVerses()
    {
        var payload = Encoding.ASCII.GetBytes("\u000bbeginning G");
        using var stream = CreateCompressedFixture(payload, blockSize: 6);

        var verses = await ReadAllVersesAsync(stream);

        Assert.Equal("beginning G", Assert.Single(verses).Text);
    }

    [Fact]
    public async Task ReadVersesAsync_UnsupportedCompression_ThrowsNotSupportedException()
    {
        var attributes = Yes2TestFile.Map(("compression.name", Yes2TestFile.EncodeString("lz4")));
        using var stream = Yes2TestFile.CreateWithAttributes(
            ("versionInfo", null, Yes2TestFile.MetadataMap()),
            ("booksInfo", null, Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", attributes, [1, (byte)'A']));

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await foreach (var _ in _parser.ReadVersesAsync(stream))
            {
            }
        });
    }

    [Fact]
    public async Task ReadVersesAsync_InvalidSnappyBlock_ThrowsYesFileCorruptException()
    {
        var attributes = Yes2TestFile.SnappyBlocksAttributes(32, 3);
        using var stream = Yes2TestFile.CreateWithAttributes(
            ("versionInfo", null, Yes2TestFile.MetadataMap()),
            ("booksInfo", null, Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", attributes, [0x40, 0x10, (byte)'a']));

        await Assert.ThrowsAsync<YesFileCorruptException>(async () =>
        {
            await foreach (var _ in _parser.ReadVersesAsync(stream))
            {
            }
        });
    }

    private static MemoryStream CreateCompressedFixture(byte[] payload, int blockSize)
    {
        var blocks = new List<byte[]>();
        foreach (var chunk in payload.Chunk(blockSize))
        {
            blocks.Add(Yes2TestFile.SnappyLiteral(chunk));
        }

        var attributes = Yes2TestFile.SnappyBlocksAttributes(blockSize, [.. blocks.Select(static block => block.Length)]);
        return Yes2TestFile.CreateWithAttributes(
            ("versionInfo", null, Yes2TestFile.MetadataMap()),
            ("booksInfo", null, Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", attributes, [.. blocks.SelectMany(static block => block)]));
    }

    private async Task<List<OpenBible.Domain.Entities.Verse>> ReadAllVersesAsync(Stream stream)
    {
        var verses = new List<OpenBible.Domain.Entities.Verse>();
        await foreach (var verse in _parser.ReadVersesAsync(stream))
        {
            verses.Add(verse);
        }

        return verses;
    }
}
