using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed class FootnoteEntry
{
    public VerseReference Reference { get; }
    public int Index { get; }
    public string Content { get; }

    public FootnoteEntry(VerseReference reference, int index, string content)
    {
        Reference = reference;
        Index = index;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }
}
