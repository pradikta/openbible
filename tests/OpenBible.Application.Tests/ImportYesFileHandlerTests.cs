using System.Text;
using NSubstitute;
using OpenBible.Application.BibleVersions.Commands.ImportYesFile;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class ImportYesFileHandlerTests
{
    private readonly IYesFileReader _yesFileReader;
    private readonly IBibleRepository _bibleRepository;
    private readonly ImportYesFileHandler _handler;

    public ImportYesFileHandlerTests()
    {
        _yesFileReader = Substitute.For<IYesFileReader>();
        _bibleRepository = Substitute.For<IBibleRepository>();
        _handler = new ImportYesFileHandler(_yesFileReader, _bibleRepository);
    }

    [Fact]
    public async Task Handle_ValidStream_ShouldSaveVersionAndReturnSuccess()
    {
        var dummyBytes = Encoding.UTF8.GetBytes("test yes content");
        using var stream = new MemoryStream(dummyBytes);

        var version = new BibleVersion("tb", "Terjemahan Baru", "id", "Indonesian translation");

        _yesFileReader.ReadMetadataAsync(stream, Arg.Any<CancellationToken>())
            .Returns(version);

        var command = new ImportYesFileCommand(stream, "tb.yes");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("tb", result.Value.Id);
        Assert.Equal("Terjemahan Baru", result.Value.Title);

        await _bibleRepository.Received(1).SaveVersionAsync(
            version,
            Arg.Any<IAsyncEnumerable<Verse>>(),
            Arg.Any<IAsyncEnumerable<Pericope>>(),
            Arg.Any<CancellationToken>()
        );
    }
}
