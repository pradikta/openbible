namespace OpenBible.Infrastructure.Search;

internal static class SearchTermMatcher
{
    /// <summary>Returns true when the verse text satisfies every term.</summary>
    public static bool MatchesAll(string? verseText, IReadOnlyList<SearchTerm> terms)
    {
        if (terms.Count == 0)
        {
            return false;
        }

        var normalized = SearchTextNormalizer.Normalize(verseText);
        var wordText = $" {string.Join(' ', SearchTextNormalizer.ToWords(verseText))} ";

        foreach (var term in terms)
        {
            var matched = term.Kind switch
            {
                SearchTermKind.Substring => normalized.Contains(term.Text, StringComparison.Ordinal),
                SearchTermKind.WholeWord or SearchTermKind.Phrase =>
                    wordText.Contains($" {term.Text} ", StringComparison.Ordinal),
                _ => false,
            };

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }
}
