using System.Text.Json;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Persistence.Serialization;
using OpenBible.Infrastructure.Persistence.Storage;

namespace OpenBible.Infrastructure.Persistence;

/// <summary>
/// Keeps installed versions in memory for fast chapter reads and writes each version as a JSON
/// snapshot to an <see cref="IPersistentStore"/> so it survives a reload.
/// </summary>
public sealed class PersistentBibleRepository(InMemoryBibleRepository inner, IPersistentStore store) : IBibleRepository
{
    private const string IndexKey = "openbible.versions";
    private const string VersionKeyPrefix = "openbible.version.";

    public Task<IReadOnlyList<BibleVersion>> GetInstalledVersionsAsync(CancellationToken ct = default) =>
        inner.GetInstalledVersionsAsync(ct);

    public Task<BibleVersion?> GetVersionAsync(string versionId, CancellationToken ct = default) =>
        inner.GetVersionAsync(versionId, ct);

    public Task<IReadOnlyList<Verse>> GetChapterVersesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default) =>
        inner.GetChapterVersesAsync(versionId, book, chapter, ct);

    public Task<IReadOnlyList<Pericope>> GetChapterPericopesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default) =>
        inner.GetChapterPericopesAsync(versionId, book, chapter, ct);

    public async Task SaveVersionAsync(
        BibleVersion version,
        IAsyncEnumerable<Verse> verses,
        IAsyncEnumerable<Pericope>? pericopes = null,
        CancellationToken ct = default)
    {
        var verseList = new List<Verse>();
        await foreach (var verse in verses.WithCancellation(ct).ConfigureAwait(false))
        {
            verseList.Add(verse);
        }

        var pericopeList = new List<Pericope>();
        if (pericopes is not null)
        {
            await foreach (var pericope in pericopes.WithCancellation(ct).ConfigureAwait(false))
            {
                pericopeList.Add(pericope);
            }
        }

        // Persist first so a failed write (for example quota) leaves memory and storage consistent.
        var json = JsonSerializer.Serialize(
            StoredMapper.ToStored(version, verseList, pericopeList),
            PersistenceJsonContext.Default.StoredVersion);
        await store.SetAsync(VersionKeyPrefix + version.Id, json, ct).ConfigureAwait(false);

        var ids = await ReadIndexAsync(ct).ConfigureAwait(false);
        if (!ids.Contains(version.Id, StringComparer.OrdinalIgnoreCase))
        {
            ids.Add(version.Id);
            await WriteIndexAsync(ids, ct).ConfigureAwait(false);
        }

        await inner.SaveVersionAsync(version, Enumerate(verseList), Enumerate(pericopeList), ct).ConfigureAwait(false);
    }

    public async Task DeleteVersionAsync(string versionId, CancellationToken ct = default)
    {
        await store.RemoveAsync(VersionKeyPrefix + versionId, ct).ConfigureAwait(false);

        var ids = await ReadIndexAsync(ct).ConfigureAwait(false);
        if (ids.RemoveAll(id => string.Equals(id, versionId, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            await WriteIndexAsync(ids, ct).ConfigureAwait(false);
        }

        await inner.DeleteVersionAsync(versionId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads every persisted version into memory and (re)builds its search index.
    /// A corrupt snapshot is skipped rather than failing startup.
    /// </summary>
    public async Task RestoreAsync(ISearchIndex searchIndex, CancellationToken ct = default)
    {
        foreach (var id in await ReadIndexAsync(ct).ConfigureAwait(false))
        {
            var json = await store.GetAsync(VersionKeyPrefix + id, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json))
            {
                continue;
            }

            StoredVersion? stored;
            try
            {
                stored = JsonSerializer.Deserialize(json, PersistenceJsonContext.Default.StoredVersion);
            }
            catch (JsonException)
            {
                continue;
            }

            if (stored is null)
            {
                continue;
            }

            var verses = StoredMapper.ToVerses(stored);
            await inner
                .SaveVersionAsync(StoredMapper.ToVersion(stored), Enumerate(verses), Enumerate(StoredMapper.ToPericopes(stored)), ct)
                .ConfigureAwait(false);
            await searchIndex.IndexVersionAsync(stored.Id, Enumerate(verses), ct).ConfigureAwait(false);
        }
    }

    private async Task<List<string>> ReadIndexAsync(CancellationToken ct)
    {
        var json = await store.GetAsync(IndexKey, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            return [.. JsonSerializer.Deserialize(json, PersistenceJsonContext.Default.StringArray) ?? []];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private Task WriteIndexAsync(List<string> ids, CancellationToken ct) =>
        store.SetAsync(IndexKey, JsonSerializer.Serialize(ids.ToArray(), PersistenceJsonContext.Default.StringArray), ct);

    private static async IAsyncEnumerable<T> Enumerate<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
