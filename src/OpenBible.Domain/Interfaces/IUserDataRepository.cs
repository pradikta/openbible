using OpenBible.Domain.Entities;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Interfaces;

public interface IUserDataRepository
{
    Task<ReadingPosition?> GetReadingPositionAsync(string versionId, CancellationToken ct = default);
    Task SaveReadingPositionAsync(ReadingPosition position, CancellationToken ct = default);

    Task<IReadOnlyList<Bookmark>> GetBookmarksAsync(string versionId, CancellationToken ct = default);
    Task SaveBookmarkAsync(Bookmark bookmark, CancellationToken ct = default);
    Task DeleteBookmarkAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Highlight>> GetHighlightsForChapterAsync(string versionId, VerseReference chapterReference, CancellationToken ct = default);
    Task SaveHighlightAsync(Highlight highlight, CancellationToken ct = default);
    Task DeleteHighlightAsync(Guid id, CancellationToken ct = default);
}
