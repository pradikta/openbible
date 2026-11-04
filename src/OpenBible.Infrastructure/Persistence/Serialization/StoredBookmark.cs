namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredBookmark(Guid Id, string VersionId, int Ari, DateTimeOffset CreatedAt, string? Note);
