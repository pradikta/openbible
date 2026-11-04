using OpenBible.Domain.Enums;

namespace OpenBible.Application.Search.DTOs;

public sealed record SearchResultDto(
    int Ari,
    string VersionId,
    BookId BookId,
    string BookName,
    int Chapter,
    int Verse,
    string Snippet,
    double Score
);
