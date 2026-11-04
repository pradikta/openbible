using NSubstitute;
using OpenBible.Application.BibleVersions.Queries.GetInstalledVersions;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class GetInstalledVersionsHandlerTests
{
    private readonly IBibleRepository _bibleRepository;
    private readonly GetInstalledVersionsHandler _handler;

    public GetInstalledVersionsHandlerTests()
    {
        _bibleRepository = Substitute.For<IBibleRepository>();
        _handler = new GetInstalledVersionsHandler(_bibleRepository);
    }

    [Fact]
    public async Task Handle_WhenVersionsExist_ShouldReturnSuccessWithDtos()
    {
        var versions = new List<BibleVersion>
        {
            new("kjv", "King James Version", "en", "Traditional translation", new List<Book>())
        };

        _bibleRepository.GetInstalledVersionsAsync(Arg.Any<CancellationToken>()).Returns(versions);

        var result = await _handler.Handle(new GetInstalledVersionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("kjv", result.Value[0].Id);
        Assert.Equal("King James Version", result.Value[0].Title);
    }
}
