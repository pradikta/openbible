using OpenBible.Domain.Enums;

namespace OpenBible.Application.Reading.DTOs;

public sealed record VerseDto(
    int VerseNumber,
    string Text,
    int Ari,
    bool IsBookmarked = false,
    HighlightColor? Highlight = null,
    IReadOnlyList<string>? Footnotes = null,
    IReadOnlyList<string>? XRefs = null
);
