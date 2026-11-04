using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

/// <summary>One of a small fixed set of "where I stopped" markers (slot 1..<see cref="SlotCount"/>).</summary>
public sealed record ProgressMark(int Slot, string VersionId, VerseReference Reference, DateTimeOffset SavedAt)
{
    public const int SlotCount = 5;
}