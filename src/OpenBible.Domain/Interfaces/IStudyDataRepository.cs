using OpenBible.Domain.Entities;

namespace OpenBible.Domain.Interfaces;

/// <summary>Notes, labels, progress marks and reading-plan progress.</summary>
public interface IStudyDataRepository
{
    Task<IReadOnlyList<Note>> GetNotesAsync(string versionId, CancellationToken ct = default);
    Task SaveNoteAsync(Note note, CancellationToken ct = default);
    Task DeleteNoteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Label>> GetLabelsAsync(CancellationToken ct = default);
    Task SaveLabelAsync(Label label, CancellationToken ct = default);
    Task DeleteLabelAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetMarkerLabelIdsAsync(Guid markerId, CancellationToken ct = default);
    Task SetMarkerLabelIdsAsync(Guid markerId, IReadOnlyCollection<Guid> labelIds, CancellationToken ct = default);

    Task<IReadOnlyList<ProgressMark>> GetProgressMarksAsync(CancellationToken ct = default);
    Task SaveProgressMarkAsync(ProgressMark mark, CancellationToken ct = default);
    Task ClearProgressMarkAsync(int slot, CancellationToken ct = default);

    Task<PlanProgress?> GetPlanProgressAsync(string planId, CancellationToken ct = default);
    Task SavePlanProgressAsync(PlanProgress progress, CancellationToken ct = default);
}