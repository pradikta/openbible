using System.Net;
using System.Text.RegularExpressions;
using OpenBible.Domain.Entities;

namespace OpenBible.Infrastructure.Sources;

public static class EbibleHtmlParser
{
    private static readonly Regex RedistRow = new(
        @"<tr\b[^>]*class=['""][^'""]*\bredist\b[^'""]*['""][^>]*>(?<row>.*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex DetailsLink = new(
        @"details\.php\?id=(?<id>[^'""&]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TableCells = new(
        @"<td\b[^>]*>(?<content>.*?)</td>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static IReadOnlyList<SourceTranslation> Parse(string html, string downloadUrlFormat)
    {
        var items = new Dictionary<string, SourceTranslation>(StringComparer.OrdinalIgnoreCase);
        foreach (Match rowMatch in RedistRow.Matches(html))
        {
            var row = rowMatch.Groups["row"].Value;
            var idMatch = DetailsLink.Match(row);
            if (!idMatch.Success)
            {
                continue;
            }

            var id = Uri.UnescapeDataString(idMatch.Groups["id"].Value.Trim());
            if (id.Length == 0)
            {
                continue;
            }

            var cells = TableCells.Matches(row)
                .Select(match => Clean(match.Groups["content"].Value))
                .ToList();

            var language = cells.Count > 2 ? cells[2] : string.Empty;
            if (language.Length == 0 && cells.Count > 1)
            {
                language = cells[1];
            }

            var name = cells.Count > 4 ? cells[4] : string.Empty;
            if (name.Length == 0 && cells.Count > 3)
            {
                name = cells[3];
            }

            if (name.Length == 0)
            {
                name = language.Length > 0 ? language : id;
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
                language,
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
