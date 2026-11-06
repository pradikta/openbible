using MediatR;
using NSubstitute;
using OpenBible.Application.BibleVersions.Commands.ImportYesFile;
using OpenBible.Application.BibleVersions.Commands.InstallSourceVersion;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.BibleVersions.Queries.GetSourceCatalog;
using OpenBible.Application.BibleVersions.Queries.GetSources;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class SourceHandlersTests
{
    [Fact]
    public async Task GetSources_MapsDirectoryEntries()
    {
        var directory = Substitute.For<ISourceDirectory>();
        directory.GetSourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new IBibleSource[]
            {
                new BibleSource("yes-example", "YES example", BibleFormat.Yes, "example.org", SourceCatalogKind.File)
            });

        var result = await new GetSourcesHandler(directory)
            .Handle(new GetSourcesQuery(), CancellationToken.None);

        var source = Assert.Single(result.Value!);
        Assert.Equal("yes-example", source.Id);
        Assert.Equal(BibleFormat.Yes, source.Format);
        Assert.Equal(SourceCatalogKind.File, source.CatalogKind);
    }

    [Fact]
    public async Task GetSources_WhenDirectoryFails_ReturnsFailure()
    {
        var directory = Substitute.For<ISourceDirectory>();
        directory.GetSourcesAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<IBibleSource>>(_ => throw new InvalidOperationException("offline"));

        var result = await new GetSourcesHandler(directory)
            .Handle(new GetSourcesQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("offline", result.Error);
    }

    [Fact]
    public async Task GetSourceCatalog_ForwardsSearchAndMapsEntries()
    {
        var directory = Substitute.For<ISourceDirectory>();
        directory.GetTranslationsAsync("yes-example", "james", Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new SourceTranslation("kjv", "King James Version", "en", "Public domain", "Public domain", "/api/sources/yes-example/download/kjv")
            });

        var result = await new GetSourceCatalogHandler(directory)
            .Handle(new GetSourceCatalogQuery("yes-example", "james"), CancellationToken.None);

        var translation = Assert.Single(result.Value!);
        Assert.Equal("yes-example", translation.SourceId);
        Assert.Equal("kjv", translation.Id);
        Assert.Equal("Public domain", translation.License);
        await directory.Received(1).GetTranslationsAsync("yes-example", "james", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InstallSourceVersion_DownloadsAndImportsWithMetadata()
    {
        var directory = Substitute.For<ISourceDirectory>();
        directory.DownloadAsync("yes-example", "kjv", Arg.Any<CancellationToken>())
            .Returns(new MemoryStream([1]));
        var sender = Substitute.For<ISender>();
        var dto = new BibleVersionDto("kjv", "King James Version", "en", "", 66, BibleFormat.Yes, "yes-example");
        sender.Send(Arg.Any<ImportYesFileCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<BibleVersionDto>.Success(dto));

        var result = await new InstallSourceVersionHandler(directory, sender)
            .Handle(new InstallSourceVersionCommand("yes-example", "kjv", BibleFormat.Yes, "Public domain"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await sender.Received(1).Send(
            Arg.Is<ImportYesFileCommand>(command =>
                command.FileName == "kjv"
                && command.Format == BibleFormat.Yes
                && command.SourceId == "yes-example"
                && command.License == "Public domain"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InstallSourceVersion_WhenDownloadFails_ReturnsFailure()
    {
        var directory = Substitute.For<ISourceDirectory>();
        directory.DownloadAsync("yes-example", "kjv", Arg.Any<CancellationToken>())
            .Returns<Stream>(_ => throw new InvalidOperationException("Download failed (502)."));
        var sender = Substitute.For<ISender>();

        var result = await new InstallSourceVersionHandler(directory, sender)
            .Handle(new InstallSourceVersionCommand("yes-example", "kjv", BibleFormat.Yes), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Download failed (502).", result.Error);
        await sender.DidNotReceive().Send(Arg.Any<ImportYesFileCommand>(), Arg.Any<CancellationToken>());
    }
}
