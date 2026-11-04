using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Entities;

public sealed class Book
{
    public BookId BookId { get; }
    public string ShortName { get; }
    public string FullName { get; }
    public int ChapterCount { get; }
    public IReadOnlyList<int> VerseCounts { get; }

    public Book(BookId bookId, string shortName, string fullName, int chapterCount, IReadOnlyList<int>? verseCounts = null)
    {
        BookId = bookId;
        ShortName = shortName ?? throw new ArgumentNullException(nameof(shortName));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        ChapterCount = chapterCount;
        VerseCounts = verseCounts ?? Array.Empty<int>();
    }
}
