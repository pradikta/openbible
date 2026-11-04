namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredProgressMark(int Slot, string VersionId, int Ari, DateTimeOffset SavedAt);