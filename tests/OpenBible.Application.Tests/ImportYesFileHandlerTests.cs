using System.Text;
using NSubstitute;
using OpenBible.Application.BibleVersions.Commands.ImportYesFile;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class ImportYesFileHandlerTests
{
    private readonly IBibleFormatReader _reader;
    private readonly IBibleRepository _bibleRepository;
    private readonly ImportYesFileHandler _handler;

    public ImportYesFileHandlerTests()
    {
        _reader = Substitute.For<IBibleFormatReader>();
        _reader.Format.Returns(BibleFormat.Yes);

        var selector = Substitute.For<IBibleFormatReaderSelector>();
        selector.GetReader(BibleFormat.Yes).Returns(_reader);

        _bibleRepository = Substitute.For<IBibleRepository>();
        _handler = new ImportYesFileHandler(selector, _bibleRepository);
    }

    [Fact]
    public async Task Handle_ValidStream_ShouldSaveVersionAndReturnSuccess()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test yes content"));
        var version = new BibleVersion("tb", "Terjemahan Baru", "id", "Indonesian translation");

        _reader.ReadMetadataAsync(stream, Arg.Any<CancellationToken>())
            .Returns(version);

        var command = new ImportYesFileCommand(stream, "tb.yes");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("tb", result.Value.Id);
        Assert.Equal("Terjemahan Baru", result.Value.Title);
        Assert.Equal(BibleFormat.Yes, result.Value.Format);

        await _bibleRepository.Received(1).SaveVersionAsync(
            Arg.Is<BibleVersion>(saved => saved.Id == "tb" && saved.Title == "Terjemahan Baru" && saved.Format == BibleFormat.Yes),
            Arg.Any<IAsyncEnumerable<Verse>>(),
            Arg.Any<IAsyncEnumerable<Pericope>>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Handle_WithSourceMetadata_ShouldStampFormatAndSource()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test yes content"));
        _reader.ReadMetadataAsync(stream, Arg.Any<CancellationToken>())
            .Returns(new BibleVersion("tb", "Terjemahan Baru", "id", ""));

        var result = await _handler.Handle(
            new ImportYesFileCommand(stream, "tb", null, BibleFormat.Yes, "yes-example", "Public domain"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("yes-example", result.Value!.SourceId);
        Assert.Equal("Public domain", result.Value.License);
        await _bibleRepository.Received(1).SaveVersionAsync(
            Arg.Is<BibleVersion>(saved => saved.SourceId == "yes-example" && saved.License == "Public domain"),
            Arg.Any<IAsyncEnumerable<Verse>>(),
            Arg.Any<IAsyncEnumerable<Pericope>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProgress_ShouldReportSavingAndCompletionStages()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test yes content"));
        var book = new Book(BookId.Genesis, "Gen", "Genesis", 1, new[] { 3 });
        var version = new BibleVersion("tb", "Terjemahan Baru", "id", "Indonesian translation", new[] { book });

        _reader.ReadMetadataAsync(stream, Arg.Any<CancellationToken>())
            .Returns(version);
        _reader.ReadVersesAsync(stream, Arg.Any<CancellationToken>())
            .Returns(VersesAsync(3));
        _reader.ReadPericopesAsync(stream, Arg.Any<CancellationToken>())
            .Returns(EmptyPericopesAsync());
        _bibleRepository
            .SaveVersionAsync(
                Arg.Any<BibleVersion>(),
                Arg.Any<IAsyncEnumerable<Verse>>(),
                Arg.Any<IAsyncEnumerable<Pericope>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ConsumeAsync(call.ArgAt<IAsyncEnumerable<Verse>>(1)));

        var progress = new RecordingProgress();
        var command = new ImportYesFileCommand(stream, "tb.yes", progress);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ImportStage.ReadingMetadata, progress.Reports[0].Stage);
        Assert.Contains(progress.Reports, report => report.Stage == ImportStage.SavingVerses && report.ProcessedVerses == 3);
        Assert.Contains(progress.Reports, report => report.Stage == ImportStage.Completed);
        Assert.All(
            progress.Reports.Where(report => report.Stage == ImportStage.SavingVerses),
            report => Assert.Equal(3, report.TotalVerses));
    }

    private static async IAsyncEnumerable<Verse> VersesAsync(int count)
    {
        for (var verse = 1; verse <= count; verse++)
        {
            yield return new Verse(new VerseReference(BookId.Genesis, 1, verse), $"Verse {verse}");
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Pericope> EmptyPericopesAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async Task ConsumeAsync(IAsyncEnumerable<Verse> verses)
    {
        await foreach (var _ in verses)
        {
        }
    }
}
