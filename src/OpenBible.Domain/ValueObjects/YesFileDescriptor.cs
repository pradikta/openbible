namespace OpenBible.Domain.ValueObjects;

/// <summary>
/// Value Object describing metadata and attributes of a .yes binary translation file.
/// </summary>
public sealed record YesFileDescriptor(
    string FileName,
    long FileSizeBytes,
    bool IsGzipped,
    int VersionNumber,
    string? Title = null,
    string? Description = null,
    string? Language = null
);
