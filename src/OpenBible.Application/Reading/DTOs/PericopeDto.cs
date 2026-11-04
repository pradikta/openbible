namespace OpenBible.Application.Reading.DTOs;

public sealed record PericopeDto(
    int BeforeVerse,
    string Title,
    IReadOnlyList<string> ParallelPassages
);
