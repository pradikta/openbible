using OpenBible.Domain.Enums;

namespace OpenBible.Application.UserData.DTOs;

public sealed record HighlightDto(
    Guid Id,
    string VersionId,
    int Ari,
    HighlightColor Color,
    DateTimeOffset UpdatedAt
);
