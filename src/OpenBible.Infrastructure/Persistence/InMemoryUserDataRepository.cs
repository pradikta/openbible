using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.Persistence;

public sealed class InMemoryUserDataRepository : IUserDataRepository
{
    private readonly Dictionary<string, ReadingPosition> _positions = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Bookmark> _bookmarks = [];
    private readonly List<Highlight> _highlights = [];

    public Task<ReadingPosition?> GetReadingPositionAsync(string versionId, CancellationToken ct = default)
    {
        _positions.TryGetValue(versionId, out var position);
        return Task.FromResult(position);
    }

    public Task SaveReadingPositionAsync(ReadingPosition position, CancellationToken ct = default)
    {
        _positions[position.VersionId] = position;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Bookmark>> GetBookmarksAsync(string versionId, CancellationToken ct = default)
    {
        var results = _bookmarks
            .Where(b => string.Equals(b.VersionId, versionId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Bookmark>>(results);
    }

    public Task SaveBookmarkAsync(Bookmark bookmark, CancellationToken ct = default)
    {
        _bookmarks.RemoveAll(b => b.Id == bookmark.Id || (b.VersionId == bookmark.VersionId && b.Reference == bookmark.Reference));
        _bookmarks.Add(bookmark);
        return Task.CompletedTask;
    }

    public Task DeleteBookmarkAsync(Guid id, CancellationToken ct = default)
    {
        _bookmarks.RemoveAll(b => b.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Highlight>> GetHighlightsForChapterAsync(string versionId, VerseReference chapterReference, CancellationToken ct = default)
    {
        var results = _highlights
            .Where(h => string.Equals(h.VersionId, versionId, StringComparison.OrdinalIgnoreCase)
                     && h.Reference.Book == chapterReference.Book
                     && h.Reference.Chapter == chapterReference.Chapter)
            .ToList();

        return Task.FromResult<IReadOnlyList<Highlight>>(results);
    }

    public Task SaveHighlightAsync(Highlight highlight, CancellationToken ct = default)
    {
        _highlights.RemoveAll(h => h.Id == highlight.Id || (h.VersionId == highlight.VersionId && h.Reference == highlight.Reference));
        _highlights.Add(highlight);
        return Task.CompletedTask;
    }

    public Task DeleteHighlightAsync(Guid id, CancellationToken ct = default)
    {
        _highlights.RemoveAll(h => h.Id == id);
        return Task.CompletedTask;
    }
}
