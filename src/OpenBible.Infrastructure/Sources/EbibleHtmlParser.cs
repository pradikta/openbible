using System.Net;
using System.Text.RegularExpressions;
using OpenBible.Domain.Entities;

namespace OpenBible.Infrastructure.Sources;

public static class EbibleHtmlParser
{
    private static readonly Regex RedistLink = new(
        @"<a\s+href=['""]details\.php\?id=([^'""&]+)['""][^>]*class=['""][^'""]*\bredist\b[^'""]*['""][^>]*>(.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static IReadOnlyList<SourceTranslation> Parse(string html, string downloadUrlFormat)
    {
        var items = new Dictionary<string, SourceTranslation>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in RedistLink.Matches(html))
        {
            var id = match.Groups[1].Value.Trim();
            if (id.Length == 0)
            {
                continue;
            }

            var name = Clean(match.Groups[2].Value);
            if (name.Length == 0)
            {
                name = id;
            }

            if (items.TryGetValue(id, out var existing) && existing.DisplayName.Length >= name.Length)
            {
                continue;
            }

            var url = downloadUrlFormat.Replace(
                "$TRANSLATION_ID",
                Uri.EscapeDataString(id),
                StringComparison.Ordinal);

            items[id] = new SourceTranslation(
                id,
                name,
                string.Empty,
                "Freely redistributable per the eBible.org listing; verify the translation's terms.",
                "Freely redistributable (verify the translation's terms)",
                url);
        }

        return [.. items.Values];
    }

    private static string Clean(string value)
    {
        var text = Tags.Replace(value, " ");
        text = WebUtility.HtmlDecode(text);
        return Whitespace.Replace(text, " ").Trim();
    }
}
