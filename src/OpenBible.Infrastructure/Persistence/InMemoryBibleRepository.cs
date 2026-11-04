using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Persistence;

public sealed class InMemoryBibleRepository : IBibleRepository
{
    private readonly Dictionary<string, BibleVersion> _versions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Verse>> _verses = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<Pericope>> _pericopes = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<BibleVersion>> GetInstalledVersionsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<BibleVersion>>(_versions.Values.ToList());
    }

    public Task<BibleVersion?> GetVersionAsync(string versionId, CancellationToken ct = default)
    {
        _versions.TryGetValue(versionId, out var version);
        return Task.FromResult(version);
    }

    public Task<IReadOnlyList<Verse>> GetChapterVersesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default)
    {
        if (!_verses.TryGetValue(versionId, out var versionVerses))
        {
            return Task.FromResult<IReadOnlyList<Verse>>(Array.Empty<Verse>());
        }

        var results = versionVerses
            .Where(v => v.Reference.Book == book && v.Reference.Chapter == chapter)
            .OrderBy(v => v.Reference.Verse)
            .ToList();

        return Task.FromResult<IReadOnlyList<Verse>>(results);
    }

    public Task<IReadOnlyList<Pericope>> GetChapterPericopesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default)
    {
        if (!_pericopes.TryGetValue(versionId, out var versionPericopes))
        {
            return Task.FromResult<IReadOnlyList<Pericope>>(Array.Empty<Pericope>());
        }

        var results = versionPericopes
            .Where(p => p.Reference.Book == book && p.Reference.Chapter == chapter)
            .OrderBy(p => p.Reference.Verse)
            .ToList();

        return Task.FromResult<IReadOnlyList<Pericope>>(results);
    }

    public async Task SaveVersionAsync(
        BibleVersion version,
        IAsyncEnumerable<Verse> verses,
        IAsyncEnumerable<Pericope>? pericopes = null,
        CancellationToken ct = default)
    {
        _versions[version.Id] = version;

        var verseList = new List<Verse>();
        await foreach (var verse in verses.WithCancellation(ct).ConfigureAwait(false))
        {
            verseList.Add(verse);
        }
        _verses[version.Id] = verseList;

        if (pericopes is not null)
        {
            var pericopeList = new List<Pericope>();
            await foreach (var pericope in pericopes.WithCancellation(ct).ConfigureAwait(false))
            {
                pericopeList.Add(pericope);
            }
            _pericopes[version.Id] = pericopeList;
        }
    }

    public Task DeleteVersionAsync(string versionId, CancellationToken ct = default)
    {
        _versions.Remove(versionId);
        _verses.Remove(versionId);
        _pericopes.Remove(versionId);
        return Task.CompletedTask;
    }
}
