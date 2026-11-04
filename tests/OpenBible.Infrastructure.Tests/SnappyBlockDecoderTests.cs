using System.Text;
using OpenBible.Domain.Exceptions;
using OpenBible.Infrastructure.YesFile.Compression;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SnappyBlockDecoderTests
{
    [Fact]
    public void Decode_LiteralBlock_ReturnsOriginalPayload()
    {
        var payload = Encoding.ASCII.GetBytes("In the beginning God created the heaven and the earth.");
        var block = Yes2TestFile.SnappyLiteral(payload);

        var decoded = SnappyBlockDecoder.Decode(block);

        Assert.Equal(payload, decoded);
    }

    [Fact]
    public void Decode_BlockWithCopyElements_ExpandsRepeatedData()
    {
        // Declared length 12: literal "abc" followed by copy(offset: 3, length: 9).
        byte[] block = [0x0c, 0x08, (byte)'a', (byte)'b', (byte)'c', ((9 - 4) << 2) | 0x01, 0x03];

        var decoded = SnappyBlockDecoder.Decode(block);

        Assert.Equal("abcabcabcabc", Encoding.ASCII.GetString(decoded));
    }

    [Fact]
    public void Decode_BlockWithLongCopyElement_ExpandsRepeatedData()
    {
        // Declared length 20: literal "xy" then copy-2(offset: 2, length: 18).
        byte[] block = [0x14, 0x04, (byte)'x', (byte)'y', ((18 - 1) << 2) | 0x02, 0x02, 0x00];

        var decoded = SnappyBlockDecoder.Decode(block);

        Assert.Equal("xyxyxyxyxyxyxyxyxyxy", Encoding.ASCII.GetString(decoded));
    }

    [Fact]
    public void Decode_TruncatedLiteral_ThrowsYesFileCorruptException()
    {
        byte[] block = [0x05, 0x10, (byte)'a'];

        Assert.Throws<YesFileCorruptException>(() => SnappyBlockDecoder.Decode(block));
    }

    [Fact]
    public void Decode_CopyBeforeStart_ThrowsYesFileCorruptException()
    {
        byte[] block = [0x08, ((4 - 4) << 2) | 0x01, 0x04];

        Assert.Throws<YesFileCorruptException>(() => SnappyBlockDecoder.Decode(block));
    }

    [Fact]
    public void ReadUncompressedLength_MultiBytePreamble_ReturnsDeclaredLength()
    {
        // 300 encodes as 0xAC 0x02 in Snappy's little-endian base-128 preamble.
        byte[] block = [0xac, 0x02];

        var length = SnappyBlockDecoder.ReadUncompressedLength(block, out var preambleLength);

        Assert.Equal(300, length);
        Assert.Equal(2, preambleLength);
    }
}
