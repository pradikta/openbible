namespace OpenBible.Domain.Interfaces;

/// <summary>Exports and restores user data (positions, bookmarks, highlights, notes, labels, marks, plan progress, default version).</summary>
public interface IBackupService
{
    /// <summary>Returns a self-describing JSON document with all user data.</summary>
    Task<string> ExportAsync(CancellationToken ct = default);

    /// <summary>Replaces current user data with the backup. Throws <see cref="InvalidDataException"/> if the document is not a valid backup; nothing is changed in that case.</summary>
    Task ImportAsync(string json, CancellationToken ct = default);
}
