using OpenBible.Domain.Enums;

namespace OpenBible.Application.BibleVersions.DTOs;

public sealed record BibleVersionDto(
    string Id,
    string Title,
    string Language,
    string Description,
    int TotalBooks,
    BibleFormat Format = BibleFormat.Yes,
    string? SourceId = null,
    string? License = null
);
