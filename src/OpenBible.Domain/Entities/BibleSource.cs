using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Entities;

public sealed record BibleSource(
    string Id,
    string DisplayName,
    BibleFormat Format,
    string Host,
    SourceCatalogKind CatalogKind,
    string? CatalogPath = null) : IBibleSource;
