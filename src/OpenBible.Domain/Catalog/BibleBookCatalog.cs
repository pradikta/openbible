using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Catalog;

public static class BibleBookCatalog
{
    private static readonly BibleBookInfo[] Books =
    [
        new(BookId.Genesis, "Gen", "Genesis", 50, Testament.OldTestament),
        new(BookId.Exodus, "Exo", "Exodus", 40, Testament.OldTestament),
        new(BookId.Leviticus, "Lev", "Leviticus", 27, Testament.OldTestament),
        new(BookId.Numbers, "Num", "Numbers", 36, Testament.OldTestament),
        new(BookId.Deuteronomy, "Deu", "Deuteronomy", 34, Testament.OldTestament),
        new(BookId.Joshua, "Jos", "Joshua", 24, Testament.OldTestament),
        new(BookId.Judges, "Jdg", "Judges", 21, Testament.OldTestament),
        new(BookId.Ruth, "Rut", "Ruth", 4, Testament.OldTestament),
        new(BookId.FirstSamuel, "1Sa", "1 Samuel", 31, Testament.OldTestament),
        new(BookId.SecondSamuel, "2Sa", "2 Samuel", 24, Testament.OldTestament),
        new(BookId.FirstKings, "1Ki", "1 Kings", 22, Testament.OldTestament),
        new(BookId.SecondKings, "2Ki", "2 Kings", 25, Testament.OldTestament),
        new(BookId.FirstChronicles, "1Ch", "1 Chronicles", 29, Testament.OldTestament),
        new(BookId.SecondChronicles, "2Ch", "2 Chronicles", 36, Testament.OldTestament),
        new(BookId.Ezra, "Ezr", "Ezra", 10, Testament.OldTestament),
        new(BookId.Nehemiah, "Neh", "Nehemiah", 13, Testament.OldTestament),
        new(BookId.Esther, "Est", "Esther", 10, Testament.OldTestament),
        new(BookId.Job, "Job", "Job", 42, Testament.OldTestament),
        new(BookId.Psalms, "Psa", "Psalms", 150, Testament.OldTestament),
        new(BookId.Proverbs, "Pro", "Proverbs", 31, Testament.OldTestament),
        new(BookId.Ecclesiastes, "Ecc", "Ecclesiastes", 12, Testament.OldTestament),
        new(BookId.SongOfSolomon, "Sng", "Song of Solomon", 8, Testament.OldTestament),
        new(BookId.Isaiah, "Isa", "Isaiah", 66, Testament.OldTestament),
        new(BookId.Jeremiah, "Jer", "Jeremiah", 52, Testament.OldTestament),
        new(BookId.Lamentations, "Lam", "Lamentations", 5, Testament.OldTestament),
        new(BookId.Ezekiel, "Ezk", "Ezekiel", 48, Testament.OldTestament),
        new(BookId.Daniel, "Dan", "Daniel", 12, Testament.OldTestament),
        new(BookId.Hosea, "Hos", "Hosea", 14, Testament.OldTestament),
        new(BookId.Joel, "Jol", "Joel", 3, Testament.OldTestament),
        new(BookId.Amos, "Amo", "Amos", 9, Testament.OldTestament),
        new(BookId.Obadiah, "Oba", "Obadiah", 1, Testament.OldTestament),
        new(BookId.Jonah, "Jon", "Jonah", 4, Testament.OldTestament),
        new(BookId.Micah, "Mic", "Micah", 7, Testament.OldTestament),
        new(BookId.Nahum, "Nam", "Nahum", 3, Testament.OldTestament),
        new(BookId.Habakkuk, "Hab", "Habakkuk", 3, Testament.OldTestament),
        new(BookId.Zephaniah, "Zep", "Zephaniah", 3, Testament.OldTestament),
        new(BookId.Haggai, "Hag", "Haggai", 2, Testament.OldTestament),
        new(BookId.Zechariah, "Zec", "Zechariah", 14, Testament.OldTestament),
        new(BookId.Malachi, "Mal", "Malachi", 4, Testament.OldTestament),
        new(BookId.Matthew, "Mat", "Matthew", 28, Testament.NewTestament),
        new(BookId.Mark, "Mrk", "Mark", 16, Testament.NewTestament),
        new(BookId.Luke, "Luk", "Luke", 24, Testament.NewTestament),
        new(BookId.John, "Jhn", "John", 21, Testament.NewTestament),
        new(BookId.Acts, "Act", "Acts", 28, Testament.NewTestament),
        new(BookId.Romans, "Rom", "Romans", 16, Testament.NewTestament),
        new(BookId.FirstCorinthians, "1Co", "1 Corinthians", 16, Testament.NewTestament),
        new(BookId.SecondCorinthians, "2Co", "2 Corinthians", 13, Testament.NewTestament),
        new(BookId.Galatians, "Gal", "Galatians", 6, Testament.NewTestament),
        new(BookId.Ephesians, "Eph", "Ephesians", 6, Testament.NewTestament),
        new(BookId.Philippians, "Php", "Philippians", 4, Testament.NewTestament),
        new(BookId.Colossians, "Col", "Colossians", 4, Testament.NewTestament),
        new(BookId.FirstThessalonians, "1Th", "1 Thessalonians", 5, Testament.NewTestament),
        new(BookId.SecondThessalonians, "2Th", "2 Thessalonians", 3, Testament.NewTestament),
        new(BookId.FirstTimothy, "1Ti", "1 Timothy", 6, Testament.NewTestament),
        new(BookId.SecondTimothy, "2Ti", "2 Timothy", 4, Testament.NewTestament),
        new(BookId.Titus, "Tit", "Titus", 3, Testament.NewTestament),
        new(BookId.Philemon, "Phm", "Philemon", 1, Testament.NewTestament),
        new(BookId.Hebrews, "Heb", "Hebrews", 13, Testament.NewTestament),
        new(BookId.James, "Jas", "James", 5, Testament.NewTestament),
        new(BookId.FirstPeter, "1Pe", "1 Peter", 5, Testament.NewTestament),
        new(BookId.SecondPeter, "2Pe", "2 Peter", 3, Testament.NewTestament),
        new(BookId.FirstJohn, "1Jn", "1 John", 5, Testament.NewTestament),
        new(BookId.SecondJohn, "2Jn", "2 John", 1, Testament.NewTestament),
        new(BookId.ThirdJohn, "3Jn", "3 John", 1, Testament.NewTestament),
        new(BookId.Jude, "Jud", "Jude", 1, Testament.NewTestament),
        new(BookId.Revelation, "Rev", "Revelation", 22, Testament.NewTestament)
    ];

    public static IReadOnlyList<BibleBookInfo> All => Books;

    public static BibleBookInfo Get(BookId id)
    {
        if (!Enum.IsDefined(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Book ID must be between Genesis and Revelation.");
        }

        return Books[(int)id];
    }

    public static bool TryNext(BookId book, int chapter, out BookId nextBook, out int nextChapter)
    {
        var info = Get(book);
        if (chapter < info.ChapterCount)
        {
            nextBook = book;
            nextChapter = chapter + 1;
            return true;
        }

        var index = (int)book;
        if (index < Books.Length - 1)
        {
            nextBook = Books[index + 1].Id;
            nextChapter = 1;
            return true;
        }

        nextBook = book;
        nextChapter = chapter;
        return false;
    }

    public static bool TryPrevious(BookId book, int chapter, out BookId previousBook, out int previousChapter)
    {
        if (chapter > 1)
        {
            previousBook = book;
            previousChapter = chapter - 1;
            return true;
        }

        var index = (int)book;
        if (index > 0)
        {
            var previous = Books[index - 1];
            previousBook = previous.Id;
            previousChapter = previous.ChapterCount;
            return true;
        }

        previousBook = book;
        previousChapter = chapter;
        return false;
    }
}
