using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed record Devotion(string Id, string Title, string Body, VerseReference Passage);