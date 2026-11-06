using NSubstitute;
using OpenBible.Application.BibleVersions.Queries.GetVersionMetadata;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class GetVersionMetadataHandlerTests
{
    private readonly IBibleRepository _bibleRepository = Substitute.For<IBibleRepository>();
    private readonly GetVersionMetadataHandler _handler;

    public GetVersionMetadataHandlerTests()
    {
        _handler = new GetVersionMetadataHandler(_bibleRepository);
    }

    [Fact]
    public async Task Handle_InstalledVersion_ShouldReturnMetadata()
    {
        var book = new Book(BookId.Genesis, "Gen", "Genesis", 50, new[] { 31 });
        var version = new BibleVersion("tb", "Terjemahan Baru", "id", "Indonesian translation", new[] { book });
        _bibleRepository
            .GetVersionAsync("tb", Arg.Any<CancellationToken>())
            .Returns(version);

        var result = await _handler.Handle(new GetVersionMetadataQuery("tb"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("tb", result.Value.Id);
        Assert.Equal("Terjemahan Baru", result.Value.Title);
        Assert.Equal("id", result.Value.Language);
        Assert.Equal(1, result.Value.TotalBooks);
    }

    [Fact]
    public async Task Handle_UnknownVersion_ShouldReturnFailure()
    {
        _bibleRepository
            .GetVersionAsync("missing", Arg.Any<CancellationToken>())
            .Returns((BibleVersion?)null);

        var result = await _handler.Handle(new GetVersionMetadataQuery("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("missing", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
