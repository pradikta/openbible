using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

/// <summary>
/// Pericope represents a section heading placed before a specific verse.
/// Includes optional parallel passages (cross-references to other books/chapters).
/// </summary>
public sealed class Pericope
{
    public VerseReference Reference { get; }
    public string Title { get; }
    public IReadOnlyList<string> ParallelPassages { get; }

    public Pericope(VerseReference reference, string title, IReadOnlyList<string>? parallelPassages = null)
    {
        Reference = reference;
        Title = !string.IsNullOrWhiteSpace(title) ? title : throw new ArgumentException("Pericope title cannot be empty.", nameof(title));
        ParallelPassages = parallelPassages ?? Array.Empty<string>();
    }
}
