using NSubstitute;
using OpenBible.Application.BibleVersions.Commands.SetDefaultVersion;
using OpenBible.Application.BibleVersions.Queries.GetDefaultVersion;
using OpenBible.Application.BibleVersions.Queries.GetVersionCatalog;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class VersionManagementHandlerTests
{
    private readonly IBibleRepository _bible = Substitute.For<IBibleRepository>();
    private readonly IAppSettingsRepository _settings = Substitute.For<IAppSettingsRepository>();

    private static BibleVersion Version(string id) => new(id, "Title", "en", "", new List<Book>());

    [Fact]
    public async Task SetDefault_WhenInstalled_StoresId()
    {
        _bible.GetVersionAsync("kjv", Arg.Any<CancellationToken>()).Returns(Version("kjv"));

        var result = await new SetDefaultVersionHandler(_settings, _bible)
            .Handle(new SetDefaultVersionCommand("kjv"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _settings.Received(1).SetDefaultVersionIdAsync("kjv", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefault_WhenNotInstalled_Fails()
    {
        _bible.GetVersionAsync("nope", Arg.Any<CancellationToken>()).Returns((BibleVersion?)null);

        var result = await new SetDefaultVersionHandler(_settings, _bible)
            .Handle(new SetDefaultVersionCommand("nope"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        await _settings.DidNotReceive().SetDefaultVersionIdAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDefault_WhenStoredVersionWasDeleted_ReturnsNull()
    {
        _settings.GetDefaultVersionIdAsync(Arg.Any<CancellationToken>()).Returns("gone");
        _bible.GetVersionAsync("gone", Arg.Any<CancellationToken>()).Returns((BibleVersion?)null);

        var result = await new GetDefaultVersionHandler(_settings, _bible)
            .Handle(new GetDefaultVersionQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetDefault_WhenInstalled_ReturnsId()
    {
        _settings.GetDefaultVersionIdAsync(Arg.Any<CancellationToken>()).Returns("kjv");
        _bible.GetVersionAsync("kjv", Arg.Any<CancellationToken>()).Returns(Version("kjv"));

        var result = await new GetDefaultVersionHandler(_settings, _bible)
            .Handle(new GetDefaultVersionQuery(), CancellationToken.None);

        Assert.Equal("kjv", result.Value);
    }

    [Fact]
    public async Task Catalog_FiltersCaseInsensitively()
    {
        var catalog = Substitute.For<IVersionCatalog>();
        catalog.GetAll().Returns(new[]
        {
            new CatalogVersion("a", "en", "KJV", "King James Version", "Old English", "http://x/a"),
            new CatalogVersion("b", "id", "TB", "Terjemahan Baru", "Indonesia", "http://x/b")
        });

        var result = await new GetVersionCatalogHandler(catalog)
            .Handle(new GetVersionCatalogQuery("james"), CancellationToken.None);

        Assert.Single(result.Value!);
        Assert.Equal("a", result.Value![0].PresetName);
    }
}
