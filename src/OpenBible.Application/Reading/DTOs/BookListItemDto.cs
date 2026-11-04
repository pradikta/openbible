using OpenBible.Domain.Enums;

namespace OpenBible.Application.Reading.DTOs;

public sealed record BookListItemDto(
    BookId BookId,
    string ShortName,
    string FullName,
    int ChapterCount,
    Testament Testament);
