using System.Net;
using System.Text.RegularExpressions;

namespace OpenBible.Infrastructure.Sword;

internal static class SwordMarkupCleaner
{
    private static readonly Regex Notes = new(
        @"<(note|FN|RF|RX|xref|scripRef|sn|ln)\b[^>]*>.*?</\1\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex SelfClosingNotes = new(
        @"<(note|xref|FN|RX)\b[^>]*/>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LineBreaks = new(
        @"<br\s*/?>|</p>|</div>|</chapter>|</verse>|</title>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Tags = new(@"<[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = Notes.Replace(text, " ");
        text = SelfClosingNotes.Replace(text, " ");
        text = LineBreaks.Replace(text, " ");
        text = Tags.Replace(text, " ");
        text = WebUtility.HtmlDecode(text);
        return Whitespace.Replace(text, " ").Trim();
    }
}
