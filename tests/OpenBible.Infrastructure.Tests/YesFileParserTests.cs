using OpenBible.Domain.Exceptions;
using OpenBible.Infrastructure.YesFile;
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
    public async Task ReadMetadataAsync_ValidHeader_WithZeroSections_ShouldReturnVersion()
    {
        using var stream = new MemoryStream();
        // Magic bytes 0x98, 0x58
        stream.WriteByte(0x98);
        stream.WriteByte(0x58);
        // Version 2 (2 bytes)
        stream.WriteByte(0x00);
        stream.WriteByte(0x02);
        // Section count: 0 (4 bytes)
        stream.WriteByte(0x00);
        stream.WriteByte(0x00);
        stream.WriteByte(0x00);
        stream.WriteByte(0x00);

        stream.Seek(0, SeekOrigin.Begin);

        var version = await _parser.ReadMetadataAsync(stream);

        Assert.NotNull(version);
        Assert.NotEmpty(version.Id);
    }
}
