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
    public async Task GetBookList_WithoutVersion_ReturnsSixtySixBooksInOrder()
    {
        var handler = new GetBookListHandler(Substitute.For<IBibleRepository>());
        var result = await handler.Handle(new GetBookListQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(66, result.Value.Count);
        Assert.Equal(BookId.Genesis, result.Value[0].BookId);
        Assert.Equal(BookId.Revelation, result.Value[^1].BookId);
        Assert.Equal(Testament.NewTestament, result.Value.First(book => book.BookId == BookId.Matthew).Testament);
    }

    [Fact]
    public async Task GetBookList_WithInstalledVersion_ReturnsVersionSpecificBooks()
    {
        var repository = Substitute.For<IBibleRepository>();
        var book = new Book(BookId.Genesis, "Kej", "Kejadian", 3, new[] { 31, 25, 24 });
        repository.GetVersionAsync("tb", Arg.Any<CancellationToken>())
            .Returns(new BibleVersion("tb", "Terjemahan Baru", "id", "Indonesian translation", new[] { book }));

        var handler = new GetBookListHandler(repository);
        var result = await handler.Handle(new GetBookListQuery("tb"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var installed = Assert.Single(result.Value!);
        Assert.Equal(BookId.Genesis, installed.BookId);
        Assert.Equal("Kejadian", installed.FullName);
        Assert.Equal(3, installed.ChapterCount);
        Assert.Equal(Testament.OldTestament, installed.Testament);
    }

    [Fact]
    public async Task GetBookList_WithUnknownVersion_FallsBackToCatalog()
    {
        var repository = Substitute.For<IBibleRepository>();
        repository.GetVersionAsync("missing", Arg.Any<CancellationToken>())
            .Returns((BibleVersion?)null);

        var handler = new GetBookListHandler(repository);
        var result = await handler.Handle(new GetBookListQuery("missing"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(66, result.Value!.Count);
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
