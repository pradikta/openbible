using OpenBible.Domain.Enums;

namespace OpenBible.Infrastructure.Usfm;

internal sealed record UsfmBookData(
    BookId BookId,
    string? Title,
    IReadOnlyList<UsfmChapter> Chapters,
    IReadOnlyList<UsfmPericope> Pericopes);
