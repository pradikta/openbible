using OpenBible.Domain.Enums;

namespace OpenBible.Application.Reading.DTOs;

public sealed record ChapterDto(
    BookId BookId,
    string BookName,
    int ChapterNumber,
    IReadOnlyList<VerseDto> Verses,
    IReadOnlyList<PericopeDto> Pericopes
);
