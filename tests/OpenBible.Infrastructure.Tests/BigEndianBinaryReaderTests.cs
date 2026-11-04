using OpenBible.Infrastructure.YesFile;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class BigEndianBinaryReaderTests
{
    [Fact]
    public void ReadInt32_BigEndian_ShouldParseCorrectly()
    {
        // 0x00, 0x01, 0x02, 0x03 in Big Endian = (1 << 16) | (2 << 8) | 3 = 66051
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        var value = reader.ReadInt32();

        Assert.Equal(66051, value);
    }

    [Fact]
    public void ReadString_With2ByteLength_ShouldParseUtf8Text()
    {
        // Length 5 in 2-byte big-endian: 0x00, 0x05, then "Hello"
        var bytes = new byte[] { 0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        var text = reader.ReadString();

        Assert.Equal("Hello", text);
    }

    [Fact]
    public void ReadShortString_With1ByteLength_ShouldParseUtf8Text()
    {
        // Length 2 in 1-byte: 0x02, then "id"
        var bytes = new byte[] { 0x02, 0x69, 0x64 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        var text = reader.ReadShortString();

        Assert.Equal("id", text);
    }
}
