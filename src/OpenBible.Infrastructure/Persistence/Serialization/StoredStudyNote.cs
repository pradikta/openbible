namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredStudyNote(Guid Id, string VersionId, int Ari, string Text, DateTimeOffset UpdatedAt);