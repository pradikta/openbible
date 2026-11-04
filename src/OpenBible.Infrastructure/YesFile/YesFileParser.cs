using System.IO.Compression;
using System.Runtime.CompilerServices;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Exceptions;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.YesFile.Sections;

namespace OpenBible.Infrastructure.YesFile;

public sealed class YesFileParser : IYesFileReader
{
    private static readonly IReadOnlyDictionary<int, IReadOnlyList<FootnoteEntry>> EmptyFootnotes =
        new Dictionary<int, IReadOnlyList<FootnoteEntry>>();

    private static readonly IReadOnlyDictionary<int, IReadOnlyList<XRefEntry>> EmptyXRefs =
        new Dictionary<int, IReadOnlyList<XRefEntry>>();

    public async Task<BibleVersion> ReadMetadataAsync(Stream stream, CancellationToken ct = default)
    {
        var decompressed = PrepareStream(stream);
        ct.ThrowIfCancellationRequested();
        var index = Yes2SectionIndex.Read(decompressed);
        if (!index.TryGetSection(YesFileConstants.Sections.VersionInfo, out var metadataSection) || metadataSection is null)
        {
            throw new YesFileCorruptException("YES2 file does not contain versionInfo metadata.");
        }

        decompressed.Position = metadataSection.ContentOffset;
        var metadata = BintexValueMapReader.Read(decompressed, metadataSection.ContentSize);
        var shortName = BintexValueAccess.GetString(metadata, "shortName");
        var title = BintexValueAccess.GetString(metadata, "longName") ?? shortName ?? "Imported Version";
        var language = BintexValueAccess.GetString(metadata, "locale") ?? "en";
        var description = BintexValueAccess.GetString(metadata, "description") ?? string.Empty;
        var books = ReadBooks(decompressed, index)
            .Select(static book => new Book(
                book.BookId,
                book.Abbreviation ?? book.ShortName,
                book.ShortName,
                book.ChapterCount,
                book.VerseCounts))
            .ToList();
        var declaredBookCount = BintexValueAccess.GetInt(metadata, "book_count", books.Count);
        if (declaredBookCount != books.Count)
        {
            throw new YesFileCorruptException("YES2 versionInfo book_count does not match booksInfo.");
        }

        var versionId = GenerateVersionId(shortName ?? title);
        await Task.CompletedTask.ConfigureAwait(false);
        return new BibleVersion(versionId, title, language, description, books);
    }

    public async IAsyncEnumerable<Verse> ReadVersesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var decompressed = PrepareStream(stream);
        var index = Yes2SectionIndex.Read(decompressed);
        var metadataSection = GetRequiredSection(index, YesFileConstants.Sections.VersionInfo);
        decompressed.Position = metadataSection.ContentOffset;
        var metadata = BintexValueMapReader.Read(decompressed, metadataSection.ContentSize);
        var textEncoding = BintexValueAccess.GetInt(metadata, "textEncoding", 2);
        var books = ReadBooks(decompressed, index);
        var footnotes = ReadFootnotes(decompressed, index);
        var xrefs = ReadXRefs(decompressed, index);
        var textSection = GetRequiredSection(index, YesFileConstants.Sections.VerseText);
        using var textContent = SectionContentStream.Open(decompressed, textSection);
        foreach (var book in books)
        {
            for (var chapter = 1; chapter <= book.ChapterCount; chapter++)
            {
                ct.ThrowIfCancellationRequested();
                var chapterOffset = checked(book.Offset + book.ChapterOffsets[chapter - 1]);
                if (chapterOffset < 0 || chapterOffset >= textContent.Length)
                {
                    throw new YesFileCorruptException("YES2 chapter offset is outside the text section.");
                }

                textContent.SeekTo(chapterOffset);
                for (var verse = 1; verse <= book.VerseCounts[chapter - 1]; verse++)
                {
                    ct.ThrowIfCancellationRequested();
                    var byteLength = BintexValueMapReader.ReadVarUInt(textContent.Stream);
                    if (byteLength < 0 || byteLength > textContent.Length - textContent.Position)
                    {
                        throw new YesFileCorruptException("YES2 verse text exceeds the text section boundary.");
                    }

                    var textBytes = ReadExactly(textContent.Stream, byteLength);
                    var text = textEncoding switch
                    {
                        1 => System.Text.Encoding.ASCII.GetString(textBytes),
                        2 => System.Text.Encoding.UTF8.GetString(textBytes),
                        _ => throw new YesFileCorruptException($"Unsupported YES2 text encoding: {textEncoding}.")
                    };

                    var reference = new VerseReference(book.BookId, chapter, verse);
                    var ari = reference.ToAri();
                    footnotes.TryGetValue(ari, out var verseFootnotes);
                    xrefs.TryGetValue(ari, out var verseXRefs);
                    yield return new Verse(reference, text, verseFootnotes, verseXRefs);
                }
            }
        }
    }

    public async IAsyncEnumerable<Pericope> ReadPericopesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var decompressed = PrepareStream(stream);
        var index = Yes2SectionIndex.Read(decompressed);
        if (!index.TryGetSection(YesFileConstants.Sections.Pericope, out var section) || section is null)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }

        using var content = SectionContentStream.Open(decompressed, section);
        foreach (var pericope in PericopeSectionReader.Read(content))
        {
            ct.ThrowIfCancellationRequested();
            yield return pericope;
        }
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<FootnoteEntry>> ReadFootnotes(Stream stream, Yes2SectionIndex index)
    {
        if (!index.TryGetSection(YesFileConstants.Sections.Footnotes, out var section) || section is null)
        {
            return EmptyFootnotes;
        }

        using var content = SectionContentStream.Open(stream, section);
        return FootnoteSectionReader.Read(content);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<XRefEntry>> ReadXRefs(Stream stream, Yes2SectionIndex index)
    {
        if (!index.TryGetSection(YesFileConstants.Sections.CrossReferences, out var section) || section is null)
        {
            return EmptyXRefs;
        }

        using var content = SectionContentStream.Open(stream, section);
        return XRefSectionReader.Read(content);
    }

    private const long MaxDecompressedBytes = 100L * 1024 * 1024; // 100 MB safety guard

    private static Stream PrepareStream(Stream stream)
    {
        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }

        // Check for GZip signature: 0x1F, 0x8B
        if (stream.CanSeek)
        {
            // Earlier reads on the same stream (e.g. metadata then verses) may have consumed it.
            stream.Seek(0, SeekOrigin.Begin);
            var b1 = stream.ReadByte();
            var b2 = stream.ReadByte();
            stream.Seek(0, SeekOrigin.Begin);

            if (b1 == 0x1F && b2 == 0x8B)
            {
                // GZipStream is forward-only and non-seekable, which breaks random section access.
                // Decompress into a seekable MemoryStream (bounded spool) so every section reader
                // can seek independently. The size guard prevents OOM on absurdly large inputs.
                using var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
                var decompressed = new MemoryStream();
                gzip.CopyTo(decompressed, checked((int)MaxDecompressedBytes));
                if (decompressed.Length > MaxDecompressedBytes)
                {
                    throw new YesFileCorruptException($".yes.gz decompressed size exceeds {MaxDecompressedBytes / (1024 * 1024)} MB.");
                }

                decompressed.Position = 0;
                return decompressed;
            }
        }

        return stream;
    }

    private static IReadOnlyList<ParsedBook> ReadBooks(Stream stream, Yes2SectionIndex index)
    {
        var section = GetRequiredSection(index, YesFileConstants.Sections.BookNames);
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

    private static Yes2SectionEntry GetRequiredSection(Yes2SectionIndex index, string name) =>
        index.TryGetSection(name, out var section) && section is not null
            ? section
            : throw new YesFileCorruptException($"YES2 file does not contain the '{name}' section.");

    private static byte[] ReadExactly(Stream stream, int count)
    {
        var bytes = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(bytes, offset, count - offset);
            if (read == 0) throw new EndOfStreamException("Unexpected end of YES2 verse text.");
            offset += read;
        }

        return bytes;
    }

    private static string GenerateVersionId(string title)
    {
        var cleaned = new string(title.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return string.IsNullOrWhiteSpace(cleaned) ? "version" : cleaned;
    }

    private sealed record ParsedBook(BookId BookId, string ShortName, string? Abbreviation, int Offset, int ChapterCount, int[] VerseCounts, int[] ChapterOffsets);
}
