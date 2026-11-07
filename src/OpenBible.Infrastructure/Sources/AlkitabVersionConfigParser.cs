using System.Text.Json;
using OpenBible.Domain.Entities;

namespace OpenBible.Infrastructure.Sources;

public static class AlkitabVersionConfigParser
{
    public static IReadOnlyList<SourceTranslation> Parse(string json, string downloadUrlFormat)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("presets", out var presets) || presets.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<SourceTranslation>();
        foreach (var preset in presets.EnumerateArray())
        {
            var id = Read(preset, "preset_name");
            if (id.Length == 0)
            {
                continue;
            }

            var name = Read(preset, "longName");
            if (name.Length == 0)
            {
                name = Read(preset, "shortName");
            }

            var url = string.Empty;
            if (downloadUrlFormat.Length > 0)
            {
                var escaped = Uri.EscapeDataString(id);
                url = downloadUrlFormat
                    .Replace("$TRANSLATION_ID", escaped, StringComparison.Ordinal)
                    .Replace("$PRESET_NAME", escaped, StringComparison.Ordinal);
            }

            items.Add(new SourceTranslation(
                id,
                name.Length > 0 ? name : id,
                Read(preset, "locale"),
                Read(preset, "description"),
                "Verify the publisher's terms before use.",
                url));
        }

        return items;
    }

    private static string Read(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
