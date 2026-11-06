using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.Usfm;

/// <summary>
/// Reads USFM sources packages as either a ZIP of <c>.usfm</c>/<c>.sfm</c> files or a single file.
/// Markers are flattened to plain text; unmapped book codes are skipped.
/// </summary>
public sealed class UsfmFormatReader : IBibleFormatReader
{
    public BibleFormat Format => BibleFormat.Usfm;

    public Task<BibleVersion> ReadMetadataAsync(Stream stream, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var books = LoadBooks(stream, ct);
        if (books.Count == 0)
        {
            throw new InvalidDataException("No mapped USFM books were found in the package.");
        }

        var title = books.Select(book => book.Title).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "USFM translation";
        IReadOnlyList<Book> versionBooks = [.. books.Select(BuildBook)];
        return Task.FromResult(new BibleVersion(Slug(title), title, "und", "Imported USFM translation", versionBooks, BibleFormat.Usfm));
    }

    public async IAsyncEnumerable<Verse> ReadVersesAsync(Stream stream, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var book in LoadBooks(stream, ct))
        {
            foreach (var chapter in book.Chapters)
            {
                foreach (var verse in chapter.Verses)
                {
                    ct.ThrowIfCancellationRequested();
                    yield return new Verse(new VerseReference(book.BookId, chapter.Number, verse.Number), verse.Text);
                }
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async IAsyncEnumerable<Pericope> ReadPericopesAsync(Stream stream, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var book in LoadBooks(stream, ct))
        {
            foreach (var pericope in book.Pericopes)
            {
                ct.ThrowIfCancellationRequested();
                yield return new Pericope(pericope.Reference, pericope.Title);
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static Book BuildBook(UsfmBookData data)
    {
        var catalog = BibleBookCatalog.Get(data.BookId);
        var chapterCount = data.Chapters.Count == 0 ? 0 : data.Chapters.Max(chapter => chapter.Number);
        var verseCounts = new int[chapterCount];
        foreach (var chapter in data.Chapters)
        {
            if (chapter.Number >= 1 && chapter.Number <= chapterCount && chapter.Verses.Count > 0)
            {
                verseCounts[chapter.Number - 1] = chapter.Verses.Max(verse => verse.Number);
            }
        }

        return new Book(
            data.BookId,
            catalog.ShortName,
            data.Title ?? catalog.FullName,
            chapterCount,
            verseCounts);
    }

    private static IReadOnlyList<UsfmBookData> LoadBooks(Stream stream, CancellationToken ct)
    {
        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("USFM import requires a seekable stream.");
        }

        stream.Seek(0, SeekOrigin.Begin);
        var books = new List<UsfmBookData>();

        if (IsZip(stream))
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();
                if (entry.Length == 0 || !IsUsfm(entry.Name))
                {
                    continue;
                }

                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var book = UsfmBookParser.Parse(reader.ReadToEnd());
                if (book is not null)
                {
                    books.Add(book);
                }
            }
        }
        else
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var book = UsfmBookParser.Parse(reader.ReadToEnd());
            if (book is not null)
            {
                books.Add(book);
            }
        }

        return [.. books.OrderBy(book => (int)book.BookId)];
    }

    private static bool IsZip(Stream stream)
    {
        var position = stream.Position;
        Span<byte> signature = stackalloc byte[4];
        var read = stream.Read(signature);
        stream.Position = position;
        return read == 4
            && signature[0] == 0x50
            && signature[1] == 0x4B
            && signature[2] == 0x03
            && signature[3] == 0x04;
    }

    private static bool IsUsfm(string name) =>
        name.EndsWith(".usfm", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase);

    private static string Slug(string title)
    {
        var cleaned = new string(title.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return cleaned.Length == 0 ? "usfm" : $"usfm-{cleaned}";
    }
}
