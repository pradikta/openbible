using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Entities;

public sealed record BibleSource(
    string Id,
    string DisplayName,
    BibleFormat Format,
    string Host,
    SourceCatalogKind CatalogKind,
    string? CatalogPath = null,
    string? CatalogUrl = null,
    string? CatalogFormat = null,
    string? DownloadHost = null,
    string? DownloadUrlFormat = null,
    int RefreshMinutes = 0,
    int CacheMinutes = 60) : IBibleSource;
