namespace OpenBible.Infrastructure.YesFile.Sections;

internal sealed record YesFileMetadata(
    string? ShortName,
    string? LongName,
    string Language,
    string Description,
    int TextEncoding,
    int DeclaredBookCount);
