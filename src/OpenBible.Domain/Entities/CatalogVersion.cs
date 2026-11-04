namespace OpenBible.Domain.Entities;

/// <summary>A downloadable translation listed in the bundled version catalog.</summary>
public sealed record CatalogVersion(
    string PresetName,
    string Locale,
    string ShortName,
    string LongName,
    string Description,
    string DownloadUrl);
