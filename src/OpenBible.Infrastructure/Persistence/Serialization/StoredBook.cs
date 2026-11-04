namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredBook(int BookId, string ShortName, string FullName, int ChapterCount, int[] VerseCounts);
