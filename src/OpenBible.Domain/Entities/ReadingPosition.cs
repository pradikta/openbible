using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed class ReadingPosition
{
    public string VersionId { get; }
    public VerseReference Reference { get; }
    public DateTimeOffset LastReadAt { get; }

    public ReadingPosition(string versionId, VerseReference reference, DateTimeOffset lastReadAt)
    {
        VersionId = !string.IsNullOrWhiteSpace(versionId) ? versionId : throw new ArgumentException("VersionId cannot be empty.", nameof(versionId));
        Reference = reference;
        LastReadAt = lastReadAt;
    }
}
