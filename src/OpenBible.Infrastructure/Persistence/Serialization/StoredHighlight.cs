namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredHighlight(Guid Id, string VersionId, int Ari, int Color, DateTimeOffset UpdatedAt);
