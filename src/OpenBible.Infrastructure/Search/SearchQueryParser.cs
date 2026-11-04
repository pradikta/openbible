using System.Text;

namespace OpenBible.Infrastructure.Search;

/// <summary>
/// Interprets a search query the way androidbible does: space-separated terms are ANDed,
/// ordinary terms match substrings, a leading <c>+</c> requests a whole word, and quoted
/// phrases or consecutive plus-prefixed words match a word sequence.
/// </summary>
internal static class SearchQueryParser
{
    public static IReadOnlyList<SearchTerm> Parse(string? query)
    {
        var terms = new List<SearchTerm>();
        if (string.IsNullOrWhiteSpace(query))
        {
            return terms;
        }

        var plusRun = new List<string>();

        foreach (var (text, quoted) in Tokenize(query))
        {
            if (quoted)
            {
                FlushPlusRun(plusRun, terms);
                AddPhrase(terms, text);
                continue;
            }

            if (text.StartsWith('+') && text.Length > 1)
            {
                plusRun.Add(text[1..]);
                continue;
            }

            FlushPlusRun(plusRun, terms);
            var normalized = SearchTextNormalizer.Normalize(text);
            if (normalized.Length > 0)
            {
                terms.Add(new SearchTerm(SearchTermKind.Substring, normalized));
            }
        }

        FlushPlusRun(plusRun, terms);
        return terms;
    }

    private static void FlushPlusRun(List<string> plusRun, List<SearchTerm> terms)
    {
        if (plusRun.Count == 0)
        {
            return;
        }

        var words = SearchTextNormalizer.ToWords(string.Join(' ', plusRun));
        plusRun.Clear();

        if (words.Count == 1)
        {
            terms.Add(new SearchTerm(SearchTermKind.WholeWord, words[0]));
        }
        else if (words.Count > 1)
        {
            terms.Add(new SearchTerm(SearchTermKind.Phrase, string.Join(' ', words)));
        }
    }

    private static void AddPhrase(List<SearchTerm> terms, string text)
    {
        var words = SearchTextNormalizer.ToWords(text);
        if (words.Count > 0)
        {
            terms.Add(new SearchTerm(SearchTermKind.Phrase, string.Join(' ', words)));
        }
    }

    private static IEnumerable<(string Text, bool Quoted)> Tokenize(string query)
    {
        var builder = new StringBuilder();
        var inQuote = false;

        foreach (var c in query)
        {
            if (c == '"')
            {
                if (builder.Length > 0)
                {
                    yield return (builder.ToString(), inQuote);
                    builder.Clear();
                }

                inQuote = !inQuote;
            }
            else if (!inQuote && char.IsWhiteSpace(c))
            {
                if (builder.Length > 0)
                {
                    yield return (builder.ToString(), false);
                    builder.Clear();
                }
            }
            else
            {
                builder.Append(c);
            }
        }

        if (builder.Length > 0)
        {
            yield return (builder.ToString(), inQuote);
        }
    }
}
