using OpenBible.Domain.Enums;

namespace OpenBible.Application.Study.DTOs;

public sealed record NoteDto(
    Guid Id,
    string VersionId,
    BookId BookId,
    string BookName,
    int Chapter,
    int Verse,
    string Text,
    DateTimeOffset UpdatedAt);
