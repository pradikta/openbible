using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed class Verse
{
    public VerseReference Reference { get; }
    public string Text { get; }
    public IReadOnlyList<FootnoteEntry> Footnotes { get; }
    public IReadOnlyList<XRefEntry> XRefs { get; }

    public Verse(
        VerseReference reference,
        string text,
        IReadOnlyList<FootnoteEntry>? footnotes = null,
        IReadOnlyList<XRefEntry>? xrefs = null)
    {
        Reference = reference;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Footnotes = footnotes ?? Array.Empty<FootnoteEntry>();
        XRefs = xrefs ?? Array.Empty<XRefEntry>();
    }
}
