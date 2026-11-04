using OpenBible.Domain.Enums;

namespace OpenBible.Application.UserData.DTOs;

public sealed record BookmarkDto(
    Guid Id,
    string VersionId,
    BookId BookId,
    string BookName,
    int Chapter,
    int Verse,
    int Ari,
    DateTimeOffset CreatedAt,
    string? Note
);
