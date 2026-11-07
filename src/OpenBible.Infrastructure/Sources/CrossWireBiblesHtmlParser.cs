using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using OpenBible.Domain.Entities;

namespace OpenBible.Infrastructure.Sources;

public static class CrossWireBiblesHtmlParser
{
    private static readonly Regex LanguageSection = new(
        @"<b>\s*Language:\s*(?<code>[^<]+?)\s*</b>(?<body>.*?)(?=<br\s+id=['""]lang_|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TableRow = new(
        @"<tr\b[^>]*>(?<row>.*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ModuleLink = new(
        @"modName=([^&""'<>\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TableCells = new(
        @"<td\b[^>]*>(?<content>.*?)</td>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static IReadOnlyList<SourceTranslation> Parse(string html, string downloadUrlFormat)
    {
        var items = new Dictionary<string, SourceTranslation>(StringComparer.OrdinalIgnoreCase);
        foreach (Match section in LanguageSection.Matches(html))
        {
            var code = Clean(section.Groups["code"].Value);
            var language = LanguageName(code);
            var body = section.Groups["body"].Value;
            var foundRows = false;

            foreach (Match rowMatch in TableRow.Matches(body))
            {
                var row = rowMatch.Groups["row"].Value;
                var moduleMatch = ModuleLink.Match(row);
                if (!moduleMatch.Success)
                {
                    continue;
                }

                foundRows = true;
                AddItem(items, moduleMatch.Groups[1].Value, language, row, downloadUrlFormat);
            }

            if (foundRows)
            {
                continue;
            }

            foreach (Match moduleMatch in ModuleLink.Matches(body))
            {
                AddItem(items, moduleMatch.Groups[1].Value, language, string.Empty, downloadUrlFormat);
            }
        }

        return [.. items.Values];
    }

    private static void AddItem(
        Dictionary<string, SourceTranslation> items,
        string rawId,
        string language,
        string row,
        string downloadUrlFormat)
    {
        var id = Uri.UnescapeDataString(rawId.Trim());
        if (id.Length == 0 || items.ContainsKey(id))
        {
            return;
        }

        var cells = TableCells.Matches(row)
            .Select(match => Clean(match.Groups["content"].Value))
            .ToList();

        var name = cells.Count > 1 && cells[1].Length > 0 ? cells[1] : id;
        var description = cells.Count > 2 && cells[2].Length > 0
            ? cells[2]
            : "CrossWire SWORD Bible module.";

        var url = downloadUrlFormat.Replace(
            "$TRANSLATION_ID",
            Uri.EscapeDataString(id),
            StringComparison.Ordinal);

        items[id] = new SourceTranslation(
            id,
            name,
            language,
            description,
            "Check the module .conf and CrossWire terms before use.",
            url);
    }

    private static string LanguageName(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        try
        {
            var name = CultureInfo.GetCultureInfo(code).EnglishName;
            return string.IsNullOrWhiteSpace(name) ? code : name;
        }
        catch (ArgumentException)
        {
            return code;
        }
    }

    private static string Clean(string value)
    {
        var text = Tags.Replace(value, " ");
        text = WebUtility.HtmlDecode(text);
        return Whitespace.Replace(text, " ").Trim();
    }
}
