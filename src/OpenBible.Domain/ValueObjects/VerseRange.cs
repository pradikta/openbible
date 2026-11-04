namespace OpenBible.Domain.ValueObjects;

/// <summary>
/// Immutable Value Object representing a contiguous range of verses within the same book.
/// </summary>
public readonly struct VerseRange : IEquatable<VerseRange>
{
    public VerseReference Start { get; }
    public VerseReference End { get; }

    public VerseRange(VerseReference start, VerseReference end)
    {
        if (start.Book != end.Book)
        {
            throw new ArgumentException("Start and End references must belong to the same book.", nameof(end));
        }

        if (start > end)
        {
            throw new ArgumentException("Start reference must be less than or equal to End reference.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public bool Contains(VerseReference reference)
    {
        if (reference.Book != Start.Book)
        {
            return false;
        }

        return reference >= Start && reference <= End;
    }

    public bool Equals(VerseRange other) => Start == other.Start && End == other.End;
    public override bool Equals(object? obj) => obj is VerseRange other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public override string ToString()
    {
        if (Start.Chapter == End.Chapter)
        {
            return $"{Start.Book} {Start.Chapter}:{Start.Verse}-{End.Verse}";
        }

        return $"{Start.Book} {Start.Chapter}:{Start.Verse} - {End.Chapter}:{End.Verse}";
    }

    public static bool operator ==(VerseRange left, VerseRange right) => left.Equals(right);
    public static bool operator !=(VerseRange left, VerseRange right) => !left.Equals(right);
}
