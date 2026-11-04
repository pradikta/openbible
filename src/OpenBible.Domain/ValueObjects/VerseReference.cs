using OpenBible.Domain.Enums;

namespace OpenBible.Domain.ValueObjects;

/// <summary>
/// Immutable Value Object representing a specific Book, Chapter, and Verse.
/// Directly encapsulates the 32-bit Alkitab Resource Identifier (ARI) bit-shifting logic.
/// Formula: ari = (bookId &lt;&lt; 16) | (chapter &lt;&lt; 8) | verse
/// </summary>
public readonly struct VerseReference : IEquatable<VerseReference>, IComparable<VerseReference>
{
    public BookId Book { get; }
    public int Chapter { get; }
    public int Verse { get; }

    /// <summary>
    /// Indicates whether this reference designates an entire chapter (verse == 0).
    /// </summary>
    public bool IsEntireChapter => Verse == 0;

    public VerseReference(BookId book, int chapter, int verse = 0)
    {
        if (!Enum.IsDefined(typeof(BookId), book))
        {
            throw new ArgumentOutOfRangeException(nameof(book), book, "Book ID must be a valid BookId enum value (0-65).");
        }

        if (chapter < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(chapter), chapter, "Chapter must be greater than or equal to 1.");
        }

        if (verse < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(verse), verse, "Verse must be non-negative (0 indicates the entire chapter).");
        }

        Book = book;
        Chapter = chapter;
        Verse = verse;
    }

    /// <summary>
    /// Packs this reference into a 32-bit ARI integer.
    /// </summary>
    public int ToAri()
    {
        return ((int)Book << 16) | (Chapter << 8) | Verse;
    }

    /// <summary>
    /// Unpacks a 32-bit ARI integer into a VerseReference.
    /// </summary>
    public static VerseReference FromAri(int ari)
    {
        var bookId = (BookId)((ari >> 16) & 0xFF);
        var chapter = (ari >> 8) & 0xFF;
        var verse = ari & 0xFF;

        return new VerseReference(bookId, chapter, verse);
    }

    public string ToDisplayString(string? bookName = null)
    {
        var name = !string.IsNullOrWhiteSpace(bookName) ? bookName : Book.ToString();
        return IsEntireChapter ? $"{name} {Chapter}" : $"{name} {Chapter}:{Verse}";
    }

    public override string ToString() => ToDisplayString();

    public bool Equals(VerseReference other) =>
        Book == other.Book && Chapter == other.Chapter && Verse == other.Verse;

    public override bool Equals(object? obj) =>
        obj is VerseReference other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Book, Chapter, Verse);

    public int CompareTo(VerseReference other)
    {
        var bookComparison = Book.CompareTo(other.Book);
        if (bookComparison != 0) return bookComparison;

        var chapterComparison = Chapter.CompareTo(other.Chapter);
        if (chapterComparison != 0) return chapterComparison;

        return Verse.CompareTo(other.Verse);
    }

    public static bool operator ==(VerseReference left, VerseReference right) => left.Equals(right);
    public static bool operator !=(VerseReference left, VerseReference right) => !left.Equals(right);
    public static bool operator <(VerseReference left, VerseReference right) => left.CompareTo(right) < 0;
    public static bool operator <=(VerseReference left, VerseReference right) => left.CompareTo(right) <= 0;
    public static bool operator >(VerseReference left, VerseReference right) => left.CompareTo(right) > 0;
    public static bool operator >=(VerseReference left, VerseReference right) => left.CompareTo(right) >= 0;
}
