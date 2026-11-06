using System.Text.Json;
using OpenBible.Domain.Entities;

namespace OpenBible.Infrastructure.Sources;

/// <summary>
/// Parses the operator-supplied catalog format: an optional <c>downloadUrlFormat</c> plus an
/// <c>items</c> array. Item <c>url</c> wins; otherwise the template replaces <c>$TRANSLATION_ID</c>.
/// </summary>
public static class SourceCatalogParser
{
    public static IReadOnlyList<SourceTranslation> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var urlFormat = ReadString(root, "downloadUrlFormat");

        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var translations = new List<SourceTranslation>();
        foreach (var item in items.EnumerateArray())
        {
            var id = ReadString(item, "id");
            if (id.Length == 0)
            {
                continue;
            }

            var url = ReadString(item, "url");
            if (url.Length == 0 && urlFormat.Length > 0)
            {
                url = urlFormat.Replace("$TRANSLATION_ID", Uri.EscapeDataString(id), StringComparison.Ordinal);
            }

            var name = ReadString(item, "name");
            translations.Add(new SourceTranslation(
                id,
                name.Length > 0 ? name : id,
                ReadString(item, "language"),
                ReadString(item, "description"),
                ReadNullableString(item, "license"),
                url));
        }

        return translations;
    }

    private static string ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string? ReadNullableString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
