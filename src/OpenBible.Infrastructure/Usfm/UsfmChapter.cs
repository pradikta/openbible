namespace OpenBible.Infrastructure.Usfm;

internal sealed record UsfmChapter(int Number, IReadOnlyList<UsfmVerse> Verses);
