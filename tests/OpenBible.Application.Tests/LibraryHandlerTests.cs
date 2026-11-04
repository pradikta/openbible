using NSubstitute;
using OpenBible.Application.BibleVersions.Commands.DeleteVersion;
using OpenBible.Application.Reading.Queries.GetBookList;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class LibraryHandlerTests
{
    [Fact]
    public async Task GetBookList_ReturnsSixtySixBooksInOrder()
    {
        var handler = new GetBookListHandler();
        var result = await handler.Handle(new GetBookListQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(66, result.Value.Count);
        Assert.Equal(BookId.Genesis, result.Value[0].BookId);
        Assert.Equal(BookId.Revelation, result.Value[^1].BookId);
        Assert.Equal(Testament.NewTestament, result.Value.First(book => book.BookId == BookId.Matthew).Testament);
    }

    [Fact]
    public async Task DeleteVersion_WhenMissing_ReturnsFailure()
    {
        var repository = Substitute.For<IBibleRepository>();
        repository.GetVersionAsync("missing", Arg.Any<CancellationToken>()).Returns((BibleVersion?)null);

        var handler = new DeleteVersionHandler(repository);
        var result = await handler.Handle(new DeleteVersionCommand("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
        await repository.DidNotReceive().DeleteVersionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteVersion_WhenPresent_Deletes()
    {
        var repository = Substitute.For<IBibleRepository>();
        repository.GetVersionAsync("kjv", Arg.Any<CancellationToken>())
            .Returns(new BibleVersion("kjv", "KJV", "en", "King James"));

        var handler = new DeleteVersionHandler(repository);
        var result = await handler.Handle(new DeleteVersionCommand("kjv"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await repository.Received(1).DeleteVersionAsync("kjv", Arg.Any<CancellationToken>());
    }
}
