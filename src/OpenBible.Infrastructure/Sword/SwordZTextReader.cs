using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.Sword;

internal sealed class SwordZTextReader
{
    private const int BufferRecordSize = 12;

    private readonly SwordArchive _archive;
    private readonly SwordModuleConf _conf;
    private readonly Encoding _encoding;
    private readonly SwordVersification _versification = SwordVersification.Kjv;
    private readonly SwordTestamentFiles? _oldTestament;
    private readonly SwordTestamentFiles? _newTestament;
    private readonly int _verseRecordSize;

    public SwordZTextReader(SwordArchive archive, SwordModuleConf conf)
    {
        _archive = archive;
        _conf = conf;
        _encoding = ResolveEncoding(conf.Encoding);

        if (!conf.ModDrv.Equals("ztext", StringComparison.OrdinalIgnoreCase)
            && !conf.ModDrv.Equals("ztext4", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"SWORD driver '{conf.ModDrv}' is not supported yet.");
        }

        if (!conf.CompressType.Equals("ZIP", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"SWORD compression '{conf.CompressType}' is not supported yet.");
        }

        if (!conf.Versification.Equals("KJV", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"SWORD versification '{conf.Versification}' is not supported yet.");
        }

        _verseRecordSize = conf.ModDrv.Equals("ztext4", StringComparison.OrdinalIgnoreCase) ? 12 : 10;
        _oldTestament = LoadTestament("ot");
        _newTestament = LoadTestament("nt");

        if (_oldTestament is null && _newTestament is null)
        {
            throw new InvalidDataException("SWORD module has no Old or New Testament data.");
        }
    }

    public BibleVersion ReadMetadata()
    {
        var books = new List<Book>();
        AddBooks(0, 39, _oldTestament, books);
        AddBooks(39, 27, _newTestament, books);

        var title = string.IsNullOrWhiteSpace(_conf.Title) ? _conf.Name : _conf.Title;
        return new BibleVersion(
            Slug(title),
            title,
            _conf.Language ?? "und",
            string.IsNullOrWhiteSpace(_conf.Title) ? "Imported SWORD module" : _conf.Title,
            books,
            BibleFormat.Sword,
            null,
            _conf.License);
    }

    public IEnumerable<Verse> ReadVerses(CancellationToken ct)
    {
        if (_oldTestament is not null)
        {
            foreach (var verse in ReadTestament(0, 39, _oldTestament, ct))
            {
                yield return verse;
            }
        }

        if (_newTestament is not null)
        {
            foreach (var verse in ReadTestament(39, 27, _newTestament, ct))
            {
                yield return verse;
            }
        }
    }

    private IEnumerable<Verse> ReadTestament(int startBook, int bookCount, SwordTestamentFiles files, CancellationToken ct)
    {
        for (var bookId = startBook; bookId < startBook + bookCount; bookId++)
        {
            var chapterCount = _versification.ChapterCount(bookId);
            for (var chapter = 1; chapter <= chapterCount; chapter++)
            {
                var verseCount = _versification.VerseCount(bookId, chapter);
                for (var verse = 1; verse <= verseCount; verse++)
                {
                    ct.ThrowIfCancellationRequested();
                    var text = ReadVerseText(files, _versification.GetIndex(bookId, chapter, verse));
                    if (text is not null)
                    {
                        yield return new Verse(new VerseReference((BookId)bookId, chapter, verse), text);
                    }
                }
            }
        }
    }

    private string? ReadVerseText(SwordTestamentFiles files, int index)
    {
        var recordOffset = (long)index * _verseRecordSize;
        if (recordOffset < 0 || recordOffset + _verseRecordSize > files.Bzv.Length)
        {
            return null;
        }

        var span = files.Bzv.AsSpan((int)recordOffset);
        var bufferNumber = BinaryPrimitives.ReadUInt32LittleEndian(span);
        var start = BinaryPrimitives.ReadUInt32LittleEndian(span[4..]);
        var length = _verseRecordSize == 12
            ? BinaryPrimitives.ReadUInt32LittleEndian(span[8..])
            : BinaryPrimitives.ReadUInt16LittleEndian(span[8..]);

        if (length == 0)
        {
            return null;
        }

        var bufferBytes = GetBufferBytes(files, bufferNumber);
        if (bufferBytes is null || (long)start + length > bufferBytes.Length)
        {
            return null;
        }

        var raw = _encoding.GetString(bufferBytes.AsSpan((int)start, (int)length));
        var cleaned = SwordMarkupCleaner.Clean(raw);
        return cleaned.Length == 0 ? null : cleaned;
    }

    private byte[]? GetBufferBytes(SwordTestamentFiles files, uint bufferNumber)
    {
        if (files.LastBufferNumber == bufferNumber && files.LastBufferBytes is not null)
        {
            return files.LastBufferBytes;
        }

        var recordOffset = (long)bufferNumber * BufferRecordSize;
        if (recordOffset < 0 || recordOffset + BufferRecordSize > files.Bzs.Length)
        {
            return null;
        }

        var span = files.Bzs.AsSpan((int)recordOffset);
        var dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(span);
        var size = BinaryPrimitives.ReadUInt32LittleEndian(span[4..]);
        if ((long)dataOffset > files.Bzz.Length || (long)dataOffset + size > files.Bzz.Length)
        {
            return null;
        }

        using var input = new MemoryStream(files.Bzz, (int)dataOffset, (int)size, writable: false);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);

        var text = output.ToArray();
        files.LastBufferNumber = bufferNumber;
        files.LastBufferBytes = text;
        return text;
    }

    private SwordTestamentFiles? LoadTestament(string testament)
    {
        var letter = _conf.BlockType.ToUpperInvariant() switch
        {
            "CHAPTER" => "c",
            "VERSE" => "v",
            _ => "b"
        };

        var basePath = $"{_conf.DataPath}/{testament}.{letter}z";
        var hasVerseIndex = _archive.TryReadFile(basePath + "v", out var bzv);
        var hasBufferIndex = _archive.TryReadFile(basePath + "s", out var bzs);
        var hasText = _archive.TryReadFile(basePath + "z", out var bzz);

        if (!hasVerseIndex && !hasBufferIndex && !hasText)
        {
            return null;
        }

        if (!hasVerseIndex || !hasBufferIndex || !hasText)
        {
            throw new InvalidDataException($"SWORD module is missing part of its {testament} data files.");
        }

        return new SwordTestamentFiles(bzv, bzs, bzz);
    }

    private void AddBooks(int startBook, int bookCount, SwordTestamentFiles? files, List<Book> books)
    {
        if (files is null)
        {
            return;
        }

        for (var bookId = startBook; bookId < startBook + bookCount; bookId++)
        {
            var catalog = BibleBookCatalog.Get((BookId)bookId);
            books.Add(new Book(
                (BookId)bookId,
                catalog.ShortName,
                catalog.FullName,
                _versification.ChapterCount(bookId),
                _versification.VerseCounts(bookId)));
        }
    }

    private static Encoding ResolveEncoding(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Encoding.UTF8;
        }

        var normalized = name.Trim().ToUpperInvariant().Replace("_", "-");
        return normalized switch
        {
            "UTF-8" or "UTF8" => Encoding.UTF8,
            "ASCII" or "US-ASCII" => Encoding.ASCII,
            "ISO-8859-1" or "LATIN1" or "LATIN-1" => Encoding.Latin1,
            "WINDOWS-1252" or "CP1252" => Encoding.Latin1,
            _ => Encoding.UTF8
        };
    }

    private static string Slug(string title)
    {
        var cleaned = new string(title.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return cleaned.Length == 0 ? "sword" : $"sword-{cleaned}";
    }
}
