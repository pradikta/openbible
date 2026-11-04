using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Catalog;

public sealed record BibleBookInfo(
    BookId Id,
    string ShortName,
    string FullName,
    int ChapterCount,
    Testament Testament);
