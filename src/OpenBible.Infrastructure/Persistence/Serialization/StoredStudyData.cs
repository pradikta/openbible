namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredStudyData(
    StoredStudyNote[] Notes,
    StoredLabel[] Labels,
    StoredMarkerLabels[] MarkerLabels,
    StoredProgressMark[] Marks,
    StoredPlanProgress[] Plans);