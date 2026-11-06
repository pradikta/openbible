namespace OpenBible.Infrastructure.Sword;

/// <summary>
/// KJV versification indices used by SWORD modules. The index space includes one entry for the
/// testament heading, one per book, and one per chapter, so verse indices are not dense.
/// </summary>
internal sealed class SwordVersification
{
    private const int OldTestamentBookCount = 39;
    private const int NewTestamentBookCount = 27;

    private readonly int[][] _books;

    public static SwordVersification Kjv { get; } = new(SwordKjvCanon.Books);

    private SwordVersification(int[][] books) => _books = books;

    public int BookCount => _books.Length;

    public int ChapterCount(int bookId) => _books[bookId].Length;

    public int VerseCount(int bookId, int chapter) => _books[bookId][chapter - 1];

    public IReadOnlyList<int> VerseCounts(int bookId) => _books[bookId];

    public int GetIndex(int bookId, int chapter, int verse)
    {
        var chapterOffset = 0;
        for (var previous = 0; previous < chapter - 1; previous++)
        {
            chapterOffset += _books[bookId][previous];
        }

        return BookOffset(bookId) + chapterOffset + chapter + verse;
    }

    public int BookOffset(int bookId)
    {
        var (start, _) = TestamentRange(bookId);
        var offset = 2;
        for (var book = start; book < bookId; book++)
        {
            offset += BookSize(book);
        }

        return offset;
    }

    public int BookSize(int bookId) => _books[bookId].Sum() + _books[bookId].Length + 1;

    public static (int Start, int Count) TestamentRange(int bookId) =>
        bookId < OldTestamentBookCount
            ? (0, OldTestamentBookCount)
            : (OldTestamentBookCount, NewTestamentBookCount);
}
