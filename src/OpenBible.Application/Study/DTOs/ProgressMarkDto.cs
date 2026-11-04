using OpenBible.Domain.Enums;

namespace OpenBible.Application.Study.DTOs;

public sealed record ProgressMarkDto(
    int Slot,
    string VersionId,
    BookId BookId,
    string BookName,
    int Chapter,
    int Verse,
    DateTimeOffset SavedAt);
