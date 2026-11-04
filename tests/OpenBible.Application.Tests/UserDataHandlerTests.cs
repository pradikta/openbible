using NSubstitute;
using OpenBible.Application.UserData.Commands.SaveReadingPosition;
using OpenBible.Application.UserData.Commands.SetVerseHighlight;
using OpenBible.Application.UserData.Commands.ToggleBookmark;
using OpenBible.Application.UserData.Queries.GetReadingPosition;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class UserDataHandlerTests
{
    private readonly IUserDataRepository _userData = Substitute.For<IUserDataRepository>();

    [Fact]
    public async Task SaveReadingPosition_PersistsReference()
    {
        var handler = new SaveReadingPositionHandler(_userData);
        var result = await handler.Handle(
            new SaveReadingPositionCommand("sample-kjv", BookId.John, 3, 16),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _userData.Received(1).SaveReadingPositionAsync(
            Arg.Is<ReadingPosition>(position =>
                position.VersionId == "sample-kjv"
                && position.Reference.Book == BookId.John
                && position.Reference.Chapter == 3
                && position.Reference.Verse == 16),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetReadingPosition_WhenMissing_ReturnsSuccessWithNull()
    {
        _userData.GetReadingPositionAsync("sample-kjv", Arg.Any<CancellationToken>())
            .Returns((ReadingPosition?)null);

        var handler = new GetReadingPositionHandler(_userData);
        var result = await handler.Handle(new GetReadingPositionQuery("sample-kjv"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task ToggleBookmark_AddsThenRemoves()
    {
        var stored = new List<Bookmark>();
        _userData.GetBookmarksAsync("sample-kjv", Arg.Any<CancellationToken>())
            .Returns(_ => (IReadOnlyList<Bookmark>)stored.ToList());
        _userData.SaveBookmarkAsync(Arg.Any<Bookmark>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                stored.Add(call.Arg<Bookmark>());
                return Task.CompletedTask;
            });
        _userData.DeleteBookmarkAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                stored.RemoveAll(bookmark => bookmark.Id == call.Arg<Guid>());
                return Task.CompletedTask;
            });

        var handler = new ToggleBookmarkHandler(_userData);
        var command = new ToggleBookmarkCommand("sample-kjv", BookId.Genesis, 1, 1);

        var added = await handler.Handle(command, CancellationToken.None);
        var removed = await handler.Handle(command, CancellationToken.None);

        Assert.True(added.IsSuccess);
        Assert.True(added.Value!.IsBookmarked);
        Assert.Equal("Genesis", added.Value.Bookmark!.BookName);
        Assert.True(removed.IsSuccess);
        Assert.False(removed.Value!.IsBookmarked);
        Assert.Empty(stored);
    }

    [Fact]
    public async Task SetVerseHighlight_SameColor_RemovesHighlight()
    {
        var reference = new VerseReference(BookId.John, 3, 16);
        var existing = new Highlight(Guid.NewGuid(), "sample-kjv", reference, HighlightColor.Yellow, DateTimeOffset.UtcNow);
        _userData.GetHighlightsForChapterAsync("sample-kjv", Arg.Any<VerseReference>(), Arg.Any<CancellationToken>())
            .Returns(new[] { existing });

        var handler = new SetVerseHighlightHandler(_userData);
        var result = await handler.Handle(
            new SetVerseHighlightCommand("sample-kjv", BookId.John, 3, 16, HighlightColor.Yellow),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
        await _userData.Received(1).DeleteHighlightAsync(existing.Id, Arg.Any<CancellationToken>());
    }
}
