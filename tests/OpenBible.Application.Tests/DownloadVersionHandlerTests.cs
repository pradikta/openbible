using MediatR;
using NSubstitute;
using OpenBible.Application.BibleVersions.Commands.DownloadVersion;
using OpenBible.Application.BibleVersions.Commands.ImportYesFile;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class DownloadVersionHandlerTests
{
    private readonly IVersionCatalog _catalog = Substitute.For<IVersionCatalog>();
    private readonly IVersionDownloader _downloader = Substitute.For<IVersionDownloader>();
    private readonly ISender _sender = Substitute.For<ISender>();

    public DownloadVersionHandlerTests() =>
        _catalog.GetAll().Returns(new[] { new CatalogVersion("kjv", "en", "KJV", "King James", "", "https://host/kjv") });

    [Fact]
    public async Task Download_UnknownPreset_Fails()
    {
        var result = await new DownloadVersionHandler(_catalog, _downloader, _sender)
            .Handle(new DownloadVersionCommand("nope"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        await _downloader.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Download_WhenDownloaderFails_ReturnsFailureMessage()
    {
        _downloader.DownloadAsync("kjv", Arg.Any<CancellationToken>())
            .Returns<Stream>(_ => throw new InvalidOperationException("No download proxy is configured."));

        var result = await new DownloadVersionHandler(_catalog, _downloader, _sender)
            .Handle(new DownloadVersionCommand("kjv"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("No download proxy is configured.", result.Error);
    }

    [Fact]
    public async Task Download_Success_ImportsDownloadedStream()
    {
        _downloader.DownloadAsync("kjv", Arg.Any<CancellationToken>()).Returns(new MemoryStream([1]));
        var dto = new BibleVersionDto("kjv", "King James", "en", "", 66);
        _sender.Send(Arg.Any<ImportYesFileCommand>(), Arg.Any<CancellationToken>()).Returns(Result<BibleVersionDto>.Success(dto));

        var result = await new DownloadVersionHandler(_catalog, _downloader, _sender)
            .Handle(new DownloadVersionCommand("kjv"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _sender.Received(1).Send(Arg.Any<ImportYesFileCommand>(), Arg.Any<CancellationToken>());
    }
}
