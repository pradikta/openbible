using System.Text.Json;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.Persistence.Serialization;
using OpenBible.Infrastructure.Persistence.Storage;

namespace OpenBible.Infrastructure.Persistence;

/// <summary>
/// Wraps <see cref="InMemoryUserDataRepository"/> and writes a snapshot of all user data
/// (reading positions, bookmarks, highlights) to an <see cref="IPersistentStore"/> after each change.
/// </summary>
public sealed class PersistentUserDataRepository(InMemoryUserDataRepository inner, IPersistentStore store) : IUserDataRepository
{
    private const string Key = "openbible.userdata";

    public Task<ReadingPosition?> GetReadingPositionAsync(string versionId, CancellationToken ct = default) =>
        inner.GetReadingPositionAsync(versionId, ct);

    public async Task SaveReadingPositionAsync(ReadingPosition position, CancellationToken ct = default)
    {
        await inner.SaveReadingPositionAsync(position, ct).ConfigureAwait(false);
        await PersistAsync(ct).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<Bookmark>> GetBookmarksAsync(string versionId, CancellationToken ct = default) =>
        inner.GetBookmarksAsync(versionId, ct);

    public async Task SaveBookmarkAsync(Bookmark bookmark, CancellationToken ct = default)
    {
        await inner.SaveBookmarkAsync(bookmark, ct).ConfigureAwait(false);
        await PersistAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteBookmarkAsync(Guid id, CancellationToken ct = default)
    {
        await inner.DeleteBookmarkAsync(id, ct).ConfigureAwait(false);
        await PersistAsync(ct).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<Highlight>> GetHighlightsForChapterAsync(string versionId, VerseReference chapterReference, CancellationToken ct = default) =>
        inner.GetHighlightsForChapterAsync(versionId, chapterReference, ct);

    public async Task SaveHighlightAsync(Highlight highlight, CancellationToken ct = default)
    {
        await inner.SaveHighlightAsync(highlight, ct).ConfigureAwait(false);
        await PersistAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteHighlightAsync(Guid id, CancellationToken ct = default)
    {
        await inner.DeleteHighlightAsync(id, ct).ConfigureAwait(false);
        await PersistAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Loads persisted user data into memory; a corrupt snapshot is ignored.</summary>
    public async Task RestoreAsync(CancellationToken ct = default)
    {
        var json = await store.GetAsync(Key, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            var stored = JsonSerializer.Deserialize(json, PersistenceJsonContext.Default.StoredUserData);
            if (stored is not null)
            {
                inner.Restore(stored);
            }
        }
        catch (JsonException)
        {
        }
    }

    internal StoredUserData Export() => inner.Export();

    /// <summary>Replaces all user data and persists the result.</summary>
    internal async Task ReplaceAsync(StoredUserData data, CancellationToken ct)
    {
        inner.Restore(data);
        await PersistAsync(ct).ConfigureAwait(false);
    }

    private Task PersistAsync(CancellationToken ct) =>
        store.SetAsync(Key, JsonSerializer.Serialize(inner.Export(), PersistenceJsonContext.Default.StoredUserData), ct);
}
