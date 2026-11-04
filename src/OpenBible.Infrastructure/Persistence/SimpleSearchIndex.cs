using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Persistence;

public sealed class SimpleSearchIndex : ISearchIndex
{
    private readonly Dictionary<string, List<Verse>> _indexedVerses = new(StringComparer.OrdinalIgnoreCase);

    public async Task IndexVersionAsync(string versionId, IAsyncEnumerable<Verse> verses, CancellationToken ct = default)
    {
        var list = new List<Verse>();
        await foreach (var verse in verses.WithCancellation(ct).ConfigureAwait(false))
        {
            list.Add(verse);
        }
        _indexedVerses[versionId] = list;
    }

    public Task<IReadOnlyList<SearchMatch>> SearchAsync(
        string versionId,
        string query,
        SearchScope? scope = null,
        CancellationToken ct = default)
    {
        if (!_indexedVerses.TryGetValue(versionId, out var verses))
        {
            return Task.FromResult<IReadOnlyList<SearchMatch>>(Array.Empty<SearchMatch>());
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var matches = new List<SearchMatch>();

        foreach (var verse in verses)
        {
            ct.ThrowIfCancellationRequested();

            if (scope?.Book is not null && verse.Reference.Book != scope.Book.Value)
            {
                continue;
            }

            if (scope?.Testament is not null)
            {
                var isOt = (int)verse.Reference.Book <= 38;
                if (scope.Testament == Domain.Enums.Testament.OldTestament && !isOt) continue;
                if (scope.Testament == Domain.Enums.Testament.NewTestament && isOt) continue;
            }

            var allTermsMatch = terms.All(t => verse.Text.Contains(t, StringComparison.OrdinalIgnoreCase));
            if (allTermsMatch)
            {
                matches.Add(new SearchMatch(verse.Reference, versionId, verse.Text, Score: 1.0));
            }
        }

        return Task.FromResult<IReadOnlyList<SearchMatch>>(matches);
    }
}
