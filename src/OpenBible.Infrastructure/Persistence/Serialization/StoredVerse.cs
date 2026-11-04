namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredVerse(int Ari, string Text, StoredNote[]? Footnotes, StoredNote[]? XRefs);
