namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredPosition(string VersionId, int Ari, DateTimeOffset LastReadAt);
