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
        using var reader = new BigEndianBinaryReader(decompressed, leaveOpen: true);

        ValidateHeader(reader);

        // Header bytes 2..7 indicate format version and flags
        var versionNumber = reader.ReadInt16();
        var sectionCount = reader.ReadInt32();

        string title = "Imported Version";
        string language = "en";
        string description = string.Empty;
        var books = new List<Book>();

        for (var i = 0; i < sectionCount; i++)
        {
            ct.ThrowIfCancellationRequested();

            var sectionName = reader.ReadShortString();
            var sectionOffset = reader.ReadInt32();
            var sectionSize = reader.ReadInt32();

            if (sectionName == YesFileConstants.Sections.VersionInfo)
            {
                var currentPos = decompressed.Position;
                if (decompressed.CanSeek)
                {
                    decompressed.Seek(sectionOffset, SeekOrigin.Begin);
                    title = reader.ReadString();
                    language = reader.ReadShortString();
                    description = reader.ReadString();
                    decompressed.Seek(currentPos, SeekOrigin.Begin);
                }
            }
        }

        var versionId = GenerateVersionId(title);
        return new BibleVersion(versionId, title, language, description, books);
    }

    public async IAsyncEnumerable<Verse> ReadVersesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var decompressed = PrepareStream(stream);
        using var reader = new BigEndianBinaryReader(decompressed, leaveOpen: true);

        ValidateHeader(reader);

        var versionNumber = reader.ReadInt16();
        var sectionCount = reader.ReadInt32();

        int verseTextOffset = 0;
        int verseTextSize = 0;

        for (var i = 0; i < sectionCount; i++)
        {
            ct.ThrowIfCancellationRequested();

            var sectionName = reader.ReadShortString();
            var sectionOffset = reader.ReadInt32();
            var sectionSize = reader.ReadInt32();

            if (sectionName == YesFileConstants.Sections.VerseText)
            {
                verseTextOffset = sectionOffset;
                verseTextSize = sectionSize;
            }
        }

        if (verseTextOffset > 0 && decompressed.CanSeek)
        {
            decompressed.Seek(verseTextOffset, SeekOrigin.Begin);

            // Read total verse count
            var totalVerses = reader.ReadInt32();

            for (var i = 0; i < totalVerses; i++)
            {
                ct.ThrowIfCancellationRequested();

                var ari = reader.ReadInt32();
                var text = reader.ReadString();

                var reference = VerseReference.FromAri(ari);
                yield return new Verse(reference, text);
            }
        }

        await Task.CompletedTask;
    }

    public async IAsyncEnumerable<Pericope> ReadPericopesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var decompressed = PrepareStream(stream);
        using var reader = new BigEndianBinaryReader(decompressed, leaveOpen: true);

        ValidateHeader(reader);

        var versionNumber = reader.ReadInt16();
        var sectionCount = reader.ReadInt32();

        int pericopeOffset = 0;

        for (var i = 0; i < sectionCount; i++)
        {
            ct.ThrowIfCancellationRequested();

            var sectionName = reader.ReadShortString();
            var sectionOffset = reader.ReadInt32();
            var sectionSize = reader.ReadInt32();

            if (sectionName == YesFileConstants.Sections.Pericope)
            {
                pericopeOffset = sectionOffset;
            }
        }

        if (pericopeOffset > 0 && decompressed.CanSeek)
        {
            decompressed.Seek(pericopeOffset, SeekOrigin.Begin);
            var pericopeCount = reader.ReadInt32();

            for (var i = 0; i < pericopeCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                var ari = reader.ReadInt32();
                var title = reader.ReadString();
                var parallelCount = reader.ReadByte();
                var parallels = new List<string>(parallelCount);

                for (var p = 0; p < parallelCount; p++)
                {
                    parallels.Add(reader.ReadString());
                }

                var reference = VerseReference.FromAri(ari);
                yield return new Pericope(reference, title, parallels);
            }
        }

        await Task.CompletedTask;
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

    private static void ValidateHeader(BigEndianBinaryReader reader)
    {
        var magic0 = reader.ReadByte();
        var magic1 = reader.ReadByte();

        if (magic0 != YesFileConstants.MagicBytes[0] || magic1 != YesFileConstants.MagicBytes[1])
        {
            throw new YesFileCorruptException(
                $"Invalid magic bytes: expected 0x98 0x58, got 0x{magic0:X2} 0x{magic1:X2}");
        }
    }

    private static string GenerateVersionId(string title)
    {
        var cleaned = new string(title.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return string.IsNullOrWhiteSpace(cleaned) ? "version" : cleaned;
    }
}
