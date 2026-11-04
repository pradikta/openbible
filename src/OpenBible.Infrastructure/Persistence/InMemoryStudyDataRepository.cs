using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.Persistence.Serialization;

namespace OpenBible.Infrastructure.Persistence;

public sealed class InMemoryStudyDataRepository : IStudyDataRepository
{
    private readonly List<Note> _notes = [];
    private readonly List<Label> _labels = [];
    private readonly Dictionary<Guid, List<Guid>> _markerLabels = [];
    private readonly Dictionary<int, ProgressMark> _marks = [];
    private readonly Dictionary<string, PlanProgress> _plans = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<Note>> GetNotesAsync(string versionId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Note>>([.. _notes
            .Where(n => string.Equals(n.VersionId, versionId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(n => n.UpdatedAt)]);

    public Task SaveNoteAsync(Note note, CancellationToken ct = default)
    {
        _notes.RemoveAll(n => n.Id == note.Id || (n.VersionId == note.VersionId && n.Reference == note.Reference));
        _notes.Add(note);
        return Task.CompletedTask;
    }

    public Task DeleteNoteAsync(Guid id, CancellationToken ct = default)
    {
        _notes.RemoveAll(n => n.Id == id);
        _markerLabels.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Label>> GetLabelsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Label>>([.. _labels.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)]);

    public Task SaveLabelAsync(Label label, CancellationToken ct = default)
    {
        _labels.RemoveAll(l => l.Id == label.Id);
        _labels.Add(label);
        return Task.CompletedTask;
    }

    public Task DeleteLabelAsync(Guid id, CancellationToken ct = default)
    {
        _labels.RemoveAll(l => l.Id == id);
        foreach (var ids in _markerLabels.Values)
        {
            ids.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> GetMarkerLabelIdsAsync(Guid markerId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(_markerLabels.TryGetValue(markerId, out var ids) ? [.. ids] : []);

    public Task SetMarkerLabelIdsAsync(Guid markerId, IReadOnlyCollection<Guid> labelIds, CancellationToken ct = default)
    {
        if (labelIds.Count == 0)
        {
            _markerLabels.Remove(markerId);
        }
        else
        {
            _markerLabels[markerId] = [.. labelIds.Distinct()];
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProgressMark>> GetProgressMarksAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProgressMark>>([.. _marks.Values.OrderBy(m => m.Slot)]);

    public Task SaveProgressMarkAsync(ProgressMark mark, CancellationToken ct = default)
    {
        _marks[mark.Slot] = mark;
        return Task.CompletedTask;
    }

    public Task ClearProgressMarkAsync(int slot, CancellationToken ct = default)
    {
        _marks.Remove(slot);
        return Task.CompletedTask;
    }

    public Task<PlanProgress?> GetPlanProgressAsync(string planId, CancellationToken ct = default)
    {
        _plans.TryGetValue(planId, out var progress);
        return Task.FromResult(progress);
    }

    public Task SavePlanProgressAsync(PlanProgress progress, CancellationToken ct = default)
    {
        _plans[progress.PlanId] = progress;
        return Task.CompletedTask;
    }

    internal StoredStudyData Export() =>
        new(
            [.. _notes.Select(n => new StoredStudyNote(n.Id, n.VersionId, n.Reference.ToAri(), n.Text, n.UpdatedAt))],
            [.. _labels.Select(l => new StoredLabel(l.Id, l.Name))],
            [.. _markerLabels.Select(p => new StoredMarkerLabels(p.Key, [.. p.Value]))],
            [.. _marks.Values.Select(m => new StoredProgressMark(m.Slot, m.VersionId, m.Reference.ToAri(), m.SavedAt))],
            [.. _plans.Values.Select(p => new StoredPlanProgress(p.PlanId, p.StartedAt, [.. p.CompletedDays]))]);

    internal void Restore(StoredStudyData data)
    {
        _notes.Clear();
        _labels.Clear();
        _markerLabels.Clear();
        _marks.Clear();
        _plans.Clear();

        _notes.AddRange(data.Notes.Select(n => new Note(n.Id, n.VersionId, VerseReference.FromAri(n.Ari), n.Text, n.UpdatedAt)));
        _labels.AddRange(data.Labels.Select(l => new Label(l.Id, l.Name)));
        foreach (var pair in data.MarkerLabels)
        {
            _markerLabels[pair.MarkerId] = [.. pair.LabelIds];
        }

        foreach (var m in data.Marks)
        {
            _marks[m.Slot] = new ProgressMark(m.Slot, m.VersionId, VerseReference.FromAri(m.Ari), m.SavedAt);
        }

        foreach (var p in data.Plans)
        {
            _plans[p.PlanId] = new PlanProgress(p.PlanId, p.StartedAt, new HashSet<int>(p.CompletedDays));
        }
    }
}