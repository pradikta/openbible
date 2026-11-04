namespace OpenBible.Infrastructure.Persistence.Serialization;

/// <summary>Footnote or cross-reference entry; the reference is the owning verse.</summary>
internal sealed record StoredNote(int Index, string Content);
