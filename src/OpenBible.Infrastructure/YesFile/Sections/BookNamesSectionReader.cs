using OpenBible.Domain.Enums;
using OpenBible.Domain.Exceptions;

namespace OpenBible.Infrastructure.YesFile.Sections;

internal static class BookNamesSectionReader
{
    public static IReadOnlyList<ParsedBook> Read(Stream stream, Yes2SectionIndex index)
    {
        if (!index.TryGetSection(YesFileConstants.Sections.BookNames, out var section) || section is null)
        {
            throw new YesFileCorruptException("YES2 file does not contain the 'booksInfo' section.");
        }

        stream.Position = section.ContentOffset;
        using var reader = new BigEndianBinaryReader(stream, leaveOpen: true);
        var count = reader.ReadInt32();
        if (count < 0 || count > 66)
        {
            throw new YesFileCorruptException("Invalid YES2 book count.");
        }

        var books = new List<ParsedBook>(count);
        var sectionEnd = section.ContentOffset + section.ContentSize;
        for (var i = 0; i < count; i++)
        {
            var values = BintexValueMapReader.ReadMap(stream);
            var bookNumber = BintexValueAccess.GetInt(values, "bookId", -1);
            if (!Enum.IsDefined(typeof(BookId), bookNumber))
            {
                throw new YesFileCorruptException($"Invalid YES2 book ID: {bookNumber}.");
            }

            var shortName = BintexValueAccess.GetString(values, "shortName") ?? ((BookId)bookNumber).ToString();
            var abbreviation = BintexValueAccess.GetString(values, "abbreviation");
            var chapterCount = BintexValueAccess.GetInt(values, "chapter_count", -1);
            var verseCounts = BintexValueAccess.GetIntArray(values, "verse_counts");
            var chapterOffsets = BintexValueAccess.GetIntArray(values, "chapter_offsets");
            var offset = BintexValueAccess.GetInt(values, "offset", -1);
            if (chapterCount < 0 || chapterCount > 255 || offset < 0 || verseCounts.Length != chapterCount || chapterOffsets.Length < chapterCount ||
                verseCounts.Any(static value => value < 0) || chapterOffsets.Any(static value => value < 0))
            {
                throw new YesFileCorruptException($"Invalid chapter index for YES2 book '{shortName}'.");
            }

            books.Add(new ParsedBook((BookId)bookNumber, shortName, abbreviation, offset, chapterCount, verseCounts, chapterOffsets));
            if (stream.Position > sectionEnd)
            {
                throw new YesFileCorruptException("YES2 booksInfo content exceeds its section boundary.");
            }
        }

        return books;
    }
}
