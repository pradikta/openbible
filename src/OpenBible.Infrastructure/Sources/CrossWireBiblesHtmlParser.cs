using System.Text.RegularExpressions;
using OpenBible.Domain.Entities;

namespace OpenBible.Infrastructure.Sources;

public static class CrossWireBiblesHtmlParser
{
    private static readonly Regex ModuleLink = new(
        @"modName=([^&""'<>\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<SourceTranslation> Parse(string html, string downloadUrlFormat)
    {
        var items = new Dictionary<string, SourceTranslation>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in ModuleLink.Matches(html))
        {
            var id = Uri.UnescapeDataString(match.Groups[1].Value.Trim());
            if (id.Length == 0 || items.ContainsKey(id))
            {
                continue;
            }

            var url = downloadUrlFormat.Replace(
                "$TRANSLATION_ID",
                Uri.EscapeDataString(id),
                StringComparison.Ordinal);

            items[id] = new SourceTranslation(
                id,
                id,
                string.Empty,
                "CrossWire SWORD Bible module.",
                "Check the module .conf and CrossWire terms before use.",
                url);
        }

        return [.. items.Values];
    }
}
