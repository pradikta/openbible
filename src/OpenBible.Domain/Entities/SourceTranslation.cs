namespace OpenBible.Domain.Entities;

public sealed record SourceTranslation(
    string Id,
    string DisplayName,
    string Language,
    string Description,
    string? License,
    string DownloadUrl);
