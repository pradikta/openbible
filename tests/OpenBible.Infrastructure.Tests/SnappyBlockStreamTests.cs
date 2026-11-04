using System.Text;
using OpenBible.Infrastructure.YesFile.Compression;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SnappyBlockStreamTests
{
    [Fact]
    public void Read_AcrossMultipleBlocks_ReturnsConcatenatedContent()
    {
        var first = Yes2TestFile.SnappyLiteral("abcd"u8);
        var second = Yes2TestFile.SnappyLiteral("efgh"u8);
        using var source = new MemoryStream([.. first, .. second]);
        using var stream = new SnappyBlockStream(source, 0, 4, [first.Length, second.Length]);
        var buffer = new byte[8];

        var read = stream.Read(buffer, 0, buffer.Length);

        Assert.Equal(8, read);
        Assert.Equal(8, stream.Length);
        Assert.Equal("abcdefgh", Encoding.ASCII.GetString(buffer));
    }

    [Fact]
    public void Position_SetAcrossBlocks_ReadsFromRequestedOffset()
    {
        var first = Yes2TestFile.SnappyLiteral("abcd"u8);
        var second = Yes2TestFile.SnappyLiteral("efgh"u8);
        using var source = new MemoryStream([.. first, .. second]);
        using var stream = new SnappyBlockStream(source, 0, 4, [first.Length, second.Length]);
        var buffer = new byte[3];

        stream.Position = 5;
        var read = stream.Read(buffer, 0, buffer.Length);

        Assert.Equal(3, read);
        Assert.Equal("fgh", Encoding.ASCII.GetString(buffer));
    }

    [Fact]
    public void Read_ContentOffsetSkipsSectionAttributes()
    {
        var block = Yes2TestFile.SnappyLiteral("data"u8);
        using var source = new MemoryStream([0xaa, 0xbb, .. block]);
        using var stream = new SnappyBlockStream(source, contentOffset: 2, blockSize: 8, [block.Length]);
        var buffer = new byte[4];

        var read = stream.Read(buffer, 0, buffer.Length);

        Assert.Equal(4, read);
        Assert.Equal("data", Encoding.ASCII.GetString(buffer));
    }
}
