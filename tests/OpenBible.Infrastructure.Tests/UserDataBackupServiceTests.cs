using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class UserDataBackupServiceTests
{
    private static readonly VerseReference Ref = new(BookId.Genesis, 1, 1);

    private sealed record Harness(
        PersistentUserDataRepository UserData,
        PersistentStudyDataRepository Study,
        PersistentAppSettingsRepository Settings,
        UserDataBackupService Backup);

    private static Harness Create()
    {
        var store = new InMemoryPersistentStore();
        var userData = new PersistentUserDataRepository(new InMemoryUserDataRepository(), store);
        var study = new PersistentStudyDataRepository(new InMemoryStudyDataRepository(), store);
        var settings = new PersistentAppSettingsRepository(store);
        return new Harness(userData, study, settings, new UserDataBackupService(userData, study, settings));
    }

    [Fact]
    public async Task ExportThenImport_RestoresAllUserData()
    {
        var source = Create();
        var bookmark = new Bookmark(Guid.NewGuid(), "v1", Ref, DateTimeOffset.UtcNow, "mark");
        var note = new Note(Guid.NewGuid(), "v1", Ref, "my note", DateTimeOffset.UtcNow);
        await source.UserData.SaveBookmarkAsync(bookmark);
        await source.Study.SaveNoteAsync(note);
        await source.Settings.SetDefaultVersionIdAsync("v1");
        var json = await source.Backup.ExportAsync();

        var target = Create();
        await target.Backup.ImportAsync(json);

        Assert.Equal(bookmark.Id, Assert.Single(await target.UserData.GetBookmarksAsync("v1")).Id);
        Assert.Equal("my note", Assert.Single(await target.Study.GetNotesAsync("v1")).Text);
        Assert.Equal("v1", await target.Settings.GetDefaultVersionIdAsync());
    }

    [Fact]
    public async Task Import_ReplacesExistingData()
    {
        var source = Create();
        var target = Create();
        await target.UserData.SaveBookmarkAsync(new Bookmark(Guid.NewGuid(), "v1", Ref, DateTimeOffset.UtcNow));

        await target.Backup.ImportAsync(await source.Backup.ExportAsync());

        Assert.Empty(await target.UserData.GetBookmarksAsync("v1"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"Format\":\"other\",\"Version\":1}")]
    [InlineData("{\"Format\":\"openbible-backup\",\"Version\":99}")]
    public async Task Import_InvalidDocument_ThrowsAndKeepsData(string json)
    {
        var target = Create();
        await target.UserData.SaveBookmarkAsync(new Bookmark(Guid.NewGuid(), "v1", Ref, DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<InvalidDataException>(() => target.Backup.ImportAsync(json));

        Assert.Single(await target.UserData.GetBookmarksAsync("v1"));
    }

    [Fact]
    public async Task Import_InvalidEntity_RollsBack()
    {
        var target = Create();
        var original = new Bookmark(Guid.NewGuid(), "v1", Ref, DateTimeOffset.UtcNow);
        await target.UserData.SaveBookmarkAsync(original);
        const string json = """
            {"Format":"openbible-backup","Version":1,"ExportedAt":"2026-01-01T00:00:00+00:00",
             "UserData":{"Positions":[],"Highlights":[],
               "Bookmarks":[{"Id":"11111111-1111-1111-1111-111111111111","VersionId":"","Ari":256,"CreatedAt":"2026-01-01T00:00:00+00:00","Note":null}]}}
            """;

        await Assert.ThrowsAsync<InvalidDataException>(() => target.Backup.ImportAsync(json));

        Assert.Equal(original.Id, Assert.Single(await target.UserData.GetBookmarksAsync("v1")).Id);
    }
}
