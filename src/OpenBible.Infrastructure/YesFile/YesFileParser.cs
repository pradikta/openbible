using System.IO.Compression;
using System.Runtime.CompilerServices;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Exceptions;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.YesFile.Sections;

namespace OpenBible.Infrastructure.YesFile;

public sealed class YesFileParser : IYesFileReader, IBibleFormatReader
{
    public BibleFormat Format => BibleFormat.Yes;
    private static readonly IReadOnlyDictionary<int, IReadOnlyList<FootnoteEntry>> EmptyFootnotes =
        new Dictionary<int, IReadOnlyList<FootnoteEntry>>();

    private static readonly IReadOnlyDictionary<int, IReadOnlyList<XRefEntry>> EmptyXRefs =
        new Dictionary<int, IReadOnlyList<XRefEntry>>();

    public async Task<BibleVersion> ReadMetadataAsync(Stream stream, CancellationToken ct = default)
    {
        var decompressed = PrepareStream(stream);
        ct.ThrowIfCancellationRequested();
        var index = Yes2SectionIndex.Read(decompressed);
        var metadata = MetadataSectionReader.Read(decompressed, index);

        var books = BookNamesSectionReader.Read(decompressed, index)
            .Select(static book => new Book(
                book.BookId,
                book.Abbreviation ?? book.ShortName,
                book.ShortName,
                book.ChapterCount,
                book.VerseCounts))
            .ToList();

        if (metadata.DeclaredBookCount >= 0 && metadata.DeclaredBookCount != books.Count)
        {
            throw new YesFileCorruptException("YES2 versionInfo book_count does not match booksInfo.");
        }

        var title = metadata.LongName ?? metadata.ShortName ?? "Imported Version";
        var versionId = GenerateVersionId(metadata.ShortName ?? title);
        await Task.CompletedTask.ConfigureAwait(false);
        return new BibleVersion(versionId, title, metadata.Language, metadata.Description, books);
    }

    public async IAsyncEnumerable<Verse> ReadVersesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var decompressed = PrepareStream(stream);
        var index = Yes2SectionIndex.Read(decompressed);
        var metadata = MetadataSectionReader.Read(decompressed, index);
        var textEncoding = metadata.TextEncoding;
        var books = BookNamesSectionReader.Read(decompressed, index);
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
}
