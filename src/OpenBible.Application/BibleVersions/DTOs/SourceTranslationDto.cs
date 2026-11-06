namespace OpenBible.Application.BibleVersions.DTOs;

public sealed record SourceTranslationDto(
    string SourceId,
    string Id,
    string DisplayName,
    string Language,
    string Description,
    string? License);
