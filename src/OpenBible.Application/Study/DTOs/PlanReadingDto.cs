using OpenBible.Domain.Enums;

namespace OpenBible.Application.Study.DTOs;

public sealed record PlanReadingDto(BookId BookId, string BookName, int Chapter, int StartVerse, int EndVerse);
