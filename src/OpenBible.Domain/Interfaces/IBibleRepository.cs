using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Interfaces;

public interface IBibleRepository
{
    Task<IReadOnlyList<BibleVersion>> GetInstalledVersionsAsync(CancellationToken ct = default);
    Task<BibleVersion?> GetVersionAsync(string versionId, CancellationToken ct = default);
    Task<IReadOnlyList<Verse>> GetChapterVersesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default);
    Task<IReadOnlyList<Pericope>> GetChapterPericopesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default);
    Task SaveVersionAsync(BibleVersion version, IAsyncEnumerable<Verse> verses, IAsyncEnumerable<Pericope>? pericopes = null, CancellationToken ct = default);
    Task DeleteVersionAsync(string versionId, CancellationToken ct = default);
}
