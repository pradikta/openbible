using OpenBible.Domain.Enums;

namespace OpenBible.Application.BibleVersions.DTOs;

public sealed record BibleSourceDto(
    string Id,
    string DisplayName,
    BibleFormat Format,
    SourceCatalogKind CatalogKind);
