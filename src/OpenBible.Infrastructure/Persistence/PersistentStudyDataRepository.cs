using System.Text.Json;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Persistence.Serialization;
using OpenBible.Infrastructure.Persistence.Storage;

namespace OpenBible.Infrastructure.Persistence;

/// <summary>Writes a snapshot of notes, labels, progress marks and plan progress after each change.</summary>
public sealed class PersistentStudyDataRepository(InMemoryStudyDataRepository inner, IPersistentStore store) : IStudyDataRepository
{
    private const string Key = "openbible.study";

    public Task<IReadOnlyList<Note>> GetNotesAsync(string versionId, CancellationToken ct = default) =>
        inner.GetNotesAsync(versionId, ct);

    public Task SaveNoteAsync(Note note, CancellationToken ct = default) =>
        MutateAsync(() => inner.SaveNoteAsync(note, ct), ct);

    public Task DeleteNoteAsync(Guid id, CancellationToken ct = default) =>
        MutateAsync(() => inner.DeleteNoteAsync(id, ct), ct);

    public Task<IReadOnlyList<Label>> GetLabelsAsync(CancellationToken ct = default) => inner.GetLabelsAsync(ct);

    public Task SaveLabelAsync(Label label, CancellationToken ct = default) =>
        MutateAsync(() => inner.SaveLabelAsync(label, ct), ct);

    public Task DeleteLabelAsync(Guid id, CancellationToken ct = default) =>
        MutateAsync(() => inner.DeleteLabelAsync(id, ct), ct);

    public Task<IReadOnlyList<Guid>> GetMarkerLabelIdsAsync(Guid markerId, CancellationToken ct = default) =>
        inner.GetMarkerLabelIdsAsync(markerId, ct);

    public Task SetMarkerLabelIdsAsync(Guid markerId, IReadOnlyCollection<Guid> labelIds, CancellationToken ct = default) =>
        MutateAsync(() => inner.SetMarkerLabelIdsAsync(markerId, labelIds, ct), ct);

    public Task<IReadOnlyList<ProgressMark>> GetProgressMarksAsync(CancellationToken ct = default) =>
        inner.GetProgressMarksAsync(ct);

    public Task SaveProgressMarkAsync(ProgressMark mark, CancellationToken ct = default) =>
        MutateAsync(() => inner.SaveProgressMarkAsync(mark, ct), ct);

    public Task ClearProgressMarkAsync(int slot, CancellationToken ct = default) =>
        MutateAsync(() => inner.ClearProgressMarkAsync(slot, ct), ct);

    public Task<PlanProgress?> GetPlanProgressAsync(string planId, CancellationToken ct = default) =>
        inner.GetPlanProgressAsync(planId, ct);

    public Task SavePlanProgressAsync(PlanProgress progress, CancellationToken ct = default) =>
        MutateAsync(() => inner.SavePlanProgressAsync(progress, ct), ct);

    /// <summary>Loads persisted study data; a corrupt snapshot is ignored.</summary>
    public async Task RestoreAsync(CancellationToken ct = default)
    {
        var json = await store.GetAsync(Key, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            var stored = JsonSerializer.Deserialize(json, PersistenceJsonContext.Default.StoredStudyData);
            if (stored is not null)
            {
                inner.Restore(stored);
            }
        }
        catch (JsonException)
        {
        }
    }

    internal StoredStudyData Export() => inner.Export();

    /// <summary>Replaces all study data and persists the result.</summary>
    internal Task ReplaceAsync(StoredStudyData data, CancellationToken ct)
    {
        inner.Restore(data);
        return store.SetAsync(Key, JsonSerializer.Serialize(inner.Export(), PersistenceJsonContext.Default.StoredStudyData), ct);
    }

    private async Task MutateAsync(Func<Task> change, CancellationToken ct)
    {
        await change().ConfigureAwait(false);
        await store.SetAsync(Key, JsonSerializer.Serialize(inner.Export(), PersistenceJsonContext.Default.StoredStudyData), ct)
            .ConfigureAwait(false);
    }
}