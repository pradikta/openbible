using NSubstitute;
using OpenBible.Application.BibleVersions.Queries.PreviewYesFile;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class PreviewYesFileHandlerTests
{
    [Fact]
    public async Task Handle_DetectedUsfm_ReturnsMetadataWithFormat()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var selector = Substitute.For<IBibleFormatReaderSelector>();
        var reader = Substitute.For<IBibleFormatReader>();
        selector.DetectFormat(stream).Returns(BibleFormat.Usfm);
        selector.GetReader(BibleFormat.Usfm).Returns(reader);
        reader.ReadMetadataAsync(stream, Arg.Any<CancellationToken>())
            .Returns(new BibleVersion("usfm-genesis", "Genesis", "und", "", null, BibleFormat.Usfm));

        var result = await new PreviewYesFileHandler(selector)
            .Handle(new PreviewYesFileQuery(stream, "genesis.zip"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BibleFormat.Usfm, result.Value!.Format);
        Assert.Equal("Genesis", result.Value.Title);
    }

    [Fact]
    public async Task Handle_UnknownFormat_ReturnsFailure()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var selector = Substitute.For<IBibleFormatReaderSelector>();
        selector.DetectFormat(stream).Returns((BibleFormat?)null);

        var result = await new PreviewYesFileHandler(selector)
            .Handle(new PreviewYesFileQuery(stream, "mystery.bin"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Unsupported", result.Error);
    }

    [Fact]
    public async Task Handle_UnsupportedReader_ReturnsFailure()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var selector = Substitute.For<IBibleFormatReaderSelector>();
        selector.DetectFormat(stream).Returns(BibleFormat.Sword);
        selector.GetReader(BibleFormat.Sword).Returns(_ => throw new NotSupportedException());

        var result = await new PreviewYesFileHandler(selector)
            .Handle(new PreviewYesFileQuery(stream, "module.zip"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not supported", result.Error);
    }
}
