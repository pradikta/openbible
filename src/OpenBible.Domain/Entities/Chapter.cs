using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Entities;

public sealed class Chapter
{
    public BookId Book { get; }
    public int ChapterNumber { get; }
    public IReadOnlyList<Verse> Verses { get; }

    public Chapter(BookId book, int chapterNumber, IReadOnlyList<Verse> verses)
    {
        if (chapterNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chapterNumber), "Chapter number must be at least 1.");
        }

        Book = book;
        ChapterNumber = chapterNumber;
        Verses = verses ?? throw new ArgumentNullException(nameof(verses));
    }
}
