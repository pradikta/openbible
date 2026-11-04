namespace OpenBible.Application.BibleVersions.DTOs;

public sealed record CatalogVersionDto(
    string PresetName,
    string Locale,
    string ShortName,
    string LongName,
    string Description,
    string DownloadUrl
);
