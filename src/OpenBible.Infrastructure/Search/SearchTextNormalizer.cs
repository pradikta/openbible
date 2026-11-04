using System.Globalization;
using System.Text;
using OpenBible.Application.Reading.Formatting;

namespace OpenBible.Infrastructure.Search;

/// <summary>
/// Normalizes query and verse text identically (invariant lower case, formatting markers
/// removed) so both sides of a comparison agree.
/// </summary>
internal static class SearchTextNormalizer
{
    /// <summary>Lower-cases and strips inline formatting markers, keeping punctuation.</summary>
    public static string Normalize(string? text) =>
        VerseMarkupFormatter.ToPlainText(text).ToLowerInvariant().Trim();

    /// <summary>Splits normalized text into words; punctuation separates words.</summary>
    public static IReadOnlyList<string> ToWords(string? text)
    {
        var words = new List<string>();
        var builder = new StringBuilder();

        foreach (var c in Normalize(text))
        {
            if (char.IsLetterOrDigit(c) || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
            else if (builder.Length > 0)
            {
                words.Add(builder.ToString());
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            words.Add(builder.ToString());
        }

        return words;
    }
}
