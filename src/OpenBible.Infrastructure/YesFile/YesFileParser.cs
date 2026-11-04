using System.IO.Compression;
using System.Runtime.CompilerServices;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Exceptions;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.YesFile;

public sealed class YesFileParser : IYesFileReader
{
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
        var shortName = GetString(metadata, "shortName");
        var title = GetString(metadata, "longName") ?? shortName ?? "Imported Version";
        var language = GetString(metadata, "locale") ?? "en";
        var description = GetString(metadata, "description") ?? string.Empty;
        var books = ReadBooks(decompressed, index)
            .Select(static book => new Book(book.BookId, book.ShortName, book.ShortName, book.ChapterCount, book.VerseCounts))
            .ToList();
        var declaredBookCount = GetInt(metadata, "book_count", books.Count);
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
        var textEncoding = GetInt(metadata, "textEncoding", 2);
        var books = ReadBooks(decompressed, index);
        var textSection = GetRequiredSection(index, YesFileConstants.Sections.VerseText);
        if (textSection.AttributesSize > 0)
        {
            decompressed.Position = textSection.StartOffset;
            var attributes = BintexValueMapReader.Read(decompressed, textSection.AttributesSize);
            if (GetString(attributes, "compression.name") is { } compression)
            {
                throw new NotSupportedException($"YES2 text compression '{compression}' is not supported yet.");
            }
        }

        var textEnd = textSection.ContentOffset + textSection.ContentSize;
        foreach (var book in books)
        {
            for (var chapter = 1; chapter <= book.ChapterCount; chapter++)
            {
                ct.ThrowIfCancellationRequested();
                var chapterOffset = book.ChapterOffsets[chapter - 1];
                var chapterPosition = checked(textSection.ContentOffset + book.Offset + chapterOffset);
                if (chapterPosition < textSection.ContentOffset || chapterPosition >= textEnd)
                {
                    throw new YesFileCorruptException("YES2 chapter offset is outside the text section.");
                }

                decompressed.Position = chapterPosition;
                for (var verse = 1; verse <= book.VerseCounts[chapter - 1]; verse++)
                {
                    ct.ThrowIfCancellationRequested();
                    var byteLength = BintexValueMapReader.ReadVarUInt(decompressed);
                    if (byteLength < 0 || byteLength > textEnd - decompressed.Position)
                    {
                        throw new YesFileCorruptException("YES2 verse text exceeds the text section boundary.");
                    }

                    var textBytes = ReadExactly(decompressed, byteLength);
                    var text = textEncoding switch
                    {
                        1 => System.Text.Encoding.ASCII.GetString(textBytes),
                        2 => System.Text.Encoding.UTF8.GetString(textBytes),
                        _ => throw new YesFileCorruptException($"Unsupported YES2 text encoding: {textEncoding}.")
                    };

                    yield return new Verse(new VerseReference(book.BookId, chapter, verse), text);
                }
            }
        }
    }

    public async IAsyncEnumerable<Pericope> ReadPericopesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var decompressed = PrepareStream(stream);
        ct.ThrowIfCancellationRequested();
        var index = Yes2SectionIndex.Read(decompressed);
        if (index.TryGetSection(YesFileConstants.Sections.Pericope, out var section) && section is not null)
        {
            throw new NotSupportedException("YES2 pericope sections are not supported yet.");
        }

        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    private static Stream PrepareStream(Stream stream)
    {
        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }

        // Check for GZip signature: 0x1F, 0x8B
        if (stream.CanSeek)
        {
            var b1 = stream.ReadByte();
            var b2 = stream.ReadByte();
            stream.Seek(0, SeekOrigin.Begin);

            if (b1 == 0x1F && b2 == 0x8B)
            {
                return new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
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
            var bookNumber = GetInt(values, "bookId", -1);
            if (!Enum.IsDefined(typeof(BookId), bookNumber))
            {
                throw new YesFileCorruptException($"Invalid YES2 book ID: {bookNumber}.");
            }

            var shortName = GetString(values, "shortName") ?? ((BookId)bookNumber).ToString();
            var chapterCount = GetInt(values, "chapter_count", -1);
            var verseCounts = GetIntArray(values, "verse_counts");
            var chapterOffsets = GetIntArray(values, "chapter_offsets");
            var offset = GetInt(values, "offset", -1);
            if (chapterCount < 0 || chapterCount > 255 || offset < 0 || verseCounts.Length != chapterCount || chapterOffsets.Length < chapterCount ||
                verseCounts.Any(static value => value < 0) || chapterOffsets.Any(static value => value < 0))
            {
                throw new YesFileCorruptException($"Invalid chapter index for YES2 book '{shortName}'.");
            }

            books.Add(new ParsedBook((BookId)bookNumber, shortName, offset, chapterCount, verseCounts, chapterOffsets));
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

    private static string? GetString(IReadOnlyDictionary<string, object?> values, string key) =>
        values.TryGetValue(key, out var value) ? value as string : null;

    private static int GetInt(IReadOnlyDictionary<string, object?> values, string key, int defaultValue) =>
        values.TryGetValue(key, out var value) && value is int integer ? integer : defaultValue;

    private static int[] GetIntArray(IReadOnlyDictionary<string, object?> values, string key) =>
        values.TryGetValue(key, out var value) && value is int[] integers ? integers : Array.Empty<int>();

    private sealed record ParsedBook(BookId BookId, string ShortName, int Offset, int ChapterCount, int[] VerseCounts, int[] ChapterOffsets);
}
