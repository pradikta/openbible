using OpenBible.Domain.Enums;

namespace OpenBible.Infrastructure.YesFile.Sections;

internal sealed record ParsedBook(
    BookId BookId,
    string ShortName,
    string? Abbreviation,
    int Offset,
    int ChapterCount,
    int[] VerseCounts,
    int[] ChapterOffsets);
