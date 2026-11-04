using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed record Note(Guid Id, string VersionId, VerseReference Reference, string Text, DateTimeOffset UpdatedAt);