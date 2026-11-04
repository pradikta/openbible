using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed class Highlight
{
    public Guid Id { get; }
    public string VersionId { get; }
    public VerseReference Reference { get; }
    public HighlightColor Color { get; }
    public DateTimeOffset UpdatedAt { get; }

    public Highlight(Guid id, string versionId, VerseReference reference, HighlightColor color, DateTimeOffset updatedAt)
    {
        Id = id;
        VersionId = !string.IsNullOrWhiteSpace(versionId) ? versionId : throw new ArgumentException("VersionId cannot be empty.", nameof(versionId));
        Reference = reference;
        Color = color;
        UpdatedAt = updatedAt;
    }
}
