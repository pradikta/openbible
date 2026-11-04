namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredMarkerLabels(Guid MarkerId, Guid[] LabelIds);