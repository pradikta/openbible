using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Interfaces;

public sealed record SearchScope(Testament? Testament = null, BookId? Book = null);

public sealed record SearchMatch(
    VerseReference Reference,
    string VersionId,
    string Text,
    double Score
);

public interface ISearchIndex
{
    Task IndexVersionAsync(string versionId, IAsyncEnumerable<Verse> verses, CancellationToken ct = default);
    Task<IReadOnlyList<SearchMatch>> SearchAsync(string versionId, string query, SearchScope? scope = null, CancellationToken ct = default);
}
