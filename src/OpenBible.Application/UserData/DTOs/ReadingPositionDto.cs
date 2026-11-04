using OpenBible.Domain.Enums;

namespace OpenBible.Application.UserData.DTOs;

public sealed record ReadingPositionDto(
    string VersionId,
    BookId BookId,
    int Chapter,
    int Verse,
    int Ari,
    DateTimeOffset LastReadAt
);
