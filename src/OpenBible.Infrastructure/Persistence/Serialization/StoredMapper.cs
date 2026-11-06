using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.Persistence.Serialization;

internal static class StoredMapper
{
    public static StoredVersion ToStored(
        BibleVersion version,
        IReadOnlyList<Verse> verses,
        IReadOnlyList<Pericope> pericopes) =>
        new(
            version.Id,
            version.Title,
            version.Language,
            version.Description,
            [.. version.Books.Select(b => new StoredBook(
                (int)b.BookId, b.ShortName, b.FullName, b.ChapterCount, [.. b.VerseCounts]))],
            [.. verses.Select(v => new StoredVerse(
                v.Reference.ToAri(),
                v.Text,
                [.. v.Footnotes.Select(f => new StoredNote(f.Index, f.Content))],
                [.. v.XRefs.Select(x => new StoredNote(x.Index, x.Content))]))],
            [.. pericopes.Select(p => new StoredPericope(p.Reference.ToAri(), p.Title, [.. p.ParallelPassages]))],
            version.Format,
            version.SourceId,
            version.License);

    public static BibleVersion ToVersion(StoredVersion stored) =>
        new(
            stored.Id,
            stored.Title,
            stored.Language,
            stored.Description,
            [.. stored.Books.Select(b => new Book((BookId)b.BookId, b.ShortName, b.FullName, b.ChapterCount, b.VerseCounts))],
            stored.Format,
            stored.SourceId,
            stored.License);

    public static List<Verse> ToVerses(StoredVersion stored) =>
        [.. stored.Verses.Select(v =>
        {
            var reference = VerseReference.FromAri(v.Ari);
            return new Verse(
                reference,
                v.Text,
                [.. (v.Footnotes ?? []).Select(n => new FootnoteEntry(reference, n.Index, n.Content))],
                [.. (v.XRefs ?? []).Select(n => new XRefEntry(reference, n.Index, n.Content))]);
        })];

    public static List<Pericope> ToPericopes(StoredVersion stored) =>
        [.. stored.Pericopes.Select(p => new Pericope(VerseReference.FromAri(p.Ari), p.Title, p.ParallelPassages))];

    public static StoredPosition ToStored(ReadingPosition p) => new(p.VersionId, p.Reference.ToAri(), p.LastReadAt);

    public static StoredBookmark ToStored(Bookmark b) => new(b.Id, b.VersionId, b.Reference.ToAri(), b.CreatedAt, b.Note);

    public static StoredHighlight ToStored(Highlight h) =>
        new(h.Id, h.VersionId, h.Reference.ToAri(), (int)h.Color, h.UpdatedAt);

    public static ReadingPosition ToDomain(StoredPosition p) =>
        new(p.VersionId, VerseReference.FromAri(p.Ari), p.LastReadAt);

    public static Bookmark ToDomain(StoredBookmark b) =>
        new(b.Id, b.VersionId, VerseReference.FromAri(b.Ari), b.CreatedAt, b.Note);

    public static Highlight ToDomain(StoredHighlight h) =>
        new(h.Id, h.VersionId, VerseReference.FromAri(h.Ari), (HighlightColor)h.Color, h.UpdatedAt);
}
