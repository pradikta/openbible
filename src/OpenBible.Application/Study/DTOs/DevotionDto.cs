using OpenBible.Domain.Enums;

namespace OpenBible.Application.Study.DTOs;

public sealed record DevotionDto(string Id, string Title, string Body, BookId BookId, string BookName, int Chapter, int Verse);
