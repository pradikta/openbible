using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Entities;

public interface IBibleSource
{
    string Id { get; }
    string DisplayName { get; }
    BibleFormat Format { get; }
    string Host { get; }
    SourceCatalogKind CatalogKind { get; }
    string? CatalogPath { get; }
    string? CatalogUrl { get; }
    string? CatalogFormat { get; }
    string? DownloadHost { get; }
    string? DownloadUrlFormat { get; }
    int RefreshMinutes { get; }
    int CacheMinutes { get; }
}
