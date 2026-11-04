using System.Text.Json;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Catalog;

/// <summary>
/// Reads the catalog shipped with the app (the upstream androidbible <c>version_config.json</c>),
/// so listing translations needs no network access.
/// </summary>
public sealed class BundledVersionCatalog : IVersionCatalog
{
    private const string ResourceName = "version_config.json";
    private readonly Lazy<IReadOnlyList<CatalogVersion>> _items = new(Load);

    public IReadOnlyList<CatalogVersion> GetAll() => _items.Value;

    private static IReadOnlyList<CatalogVersion> Load()
    {
        using var stream = typeof(BundledVersionCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var urlFormat = root.TryGetProperty("download_url_format", out var format) ? format.GetString() ?? string.Empty : string.Empty;

        var items = new List<CatalogVersion>();
        foreach (var preset in root.GetProperty("presets").EnumerateArray())
        {
            var name = Read(preset, "preset_name");
            if (name.Length == 0)
            {
                continue;
            }

            items.Add(new CatalogVersion(
                name,
                Read(preset, "locale"),
                Read(preset, "shortName"),
                Read(preset, "longName"),
                Read(preset, "description"),
                urlFormat.Replace("$PRESET_NAME", Uri.EscapeDataString(name), StringComparison.Ordinal)));
        }

        return items;
    }

    private static string Read(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
