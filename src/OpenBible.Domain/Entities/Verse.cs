using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed class Verse
{
    public VerseReference Reference { get; }
    public string Text { get; }
    public IReadOnlyList<FootnoteEntry> Footnotes { get; }

    public Verse(VerseReference reference, string text, IReadOnlyList<FootnoteEntry>? footnotes = null)
    {
        Reference = reference;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Footnotes = footnotes ?? Array.Empty<FootnoteEntry>();
    }
}
