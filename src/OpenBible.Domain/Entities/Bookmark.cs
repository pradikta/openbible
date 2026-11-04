using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed class Bookmark
{
    public Guid Id { get; }
    public string VersionId { get; }
    public VerseReference Reference { get; }
    public DateTimeOffset CreatedAt { get; }
    public string? Note { get; }

    public Bookmark(Guid id, string versionId, VerseReference reference, DateTimeOffset createdAt, string? note = null)
    {
        Id = id;
        VersionId = !string.IsNullOrWhiteSpace(versionId) ? versionId : throw new ArgumentException("VersionId cannot be empty.", nameof(versionId));
        Reference = reference;
        CreatedAt = createdAt;
        Note = note;
    }
}
