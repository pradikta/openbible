using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class PersistenceTests
{
    private static readonly VerseReference Ref = new(BookId.Genesis, 1, 1);

    private static async IAsyncEnumerable<T> Items<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Version_SurvivesRestartAndIsReindexed()
    {
        var store = new InMemoryPersistentStore();
        var first = new PersistentBibleRepository(new InMemoryBibleRepository(), store);
        await first.SaveVersionAsync(
            new BibleVersion("v1", "Title", "en", "desc"),
            Items(new Verse(Ref, "In the beginning")));

        var second = new PersistentBibleRepository(new InMemoryBibleRepository(), store);
        var index = new SimpleSearchIndex();
        await second.RestoreAsync(index);

        var verses = await second.GetChapterVersesAsync("v1", BookId.Genesis, 1);
        Assert.Single(verses);
        Assert.Equal("In the beginning", verses[0].Text);
        Assert.Equal("Title", (await second.GetVersionAsync("v1"))!.Title);
    }

    [Fact]
    public async Task DeleteVersion_RemovesPersistedData()
    {
        var store = new InMemoryPersistentStore();
        var repo = new PersistentBibleRepository(new InMemoryBibleRepository(), store);
        await repo.SaveVersionAsync(new BibleVersion("v1", "T", "en", "d"), Items(new Verse(Ref, "x")));
        await repo.DeleteVersionAsync("v1");

        var restored = new PersistentBibleRepository(new InMemoryBibleRepository(), store);
        await restored.RestoreAsync(new SimpleSearchIndex());

        Assert.Empty(await restored.GetInstalledVersionsAsync());
    }

    [Fact]
    public async Task UserData_RoundTrips()
    {
        var store = new InMemoryPersistentStore();
        var repo = new PersistentUserDataRepository(new InMemoryUserDataRepository(), store);
        var bookmark = new Bookmark(Guid.NewGuid(), "v1", Ref, DateTimeOffset.UtcNow, "note");
        var highlight = new Highlight(Guid.NewGuid(), "v1", Ref, HighlightColor.Green, DateTimeOffset.UtcNow);
        await repo.SaveBookmarkAsync(bookmark);
        await repo.SaveHighlightAsync(highlight);
        await repo.SaveReadingPositionAsync(new ReadingPosition("v1", Ref, DateTimeOffset.UtcNow));

        var restored = new PersistentUserDataRepository(new InMemoryUserDataRepository(), store);
        await restored.RestoreAsync();

        Assert.Equal(bookmark.Id, Assert.Single(await restored.GetBookmarksAsync("v1")).Id);
        Assert.Single(await restored.GetHighlightsForChapterAsync("v1", new VerseReference(BookId.Genesis, 1)));
        Assert.NotNull(await restored.GetReadingPositionAsync("v1"));

        await restored.DeleteBookmarkAsync(bookmark.Id);
        var again = new PersistentUserDataRepository(new InMemoryUserDataRepository(), store);
        await again.RestoreAsync();
        Assert.Empty(await again.GetBookmarksAsync("v1"));
    }
}
