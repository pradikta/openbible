using System.IO.Compression;
using System.Text;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.YesFile;

public sealed class BibleFormatReaderSelector : IBibleFormatReaderSelector
{
    private readonly IReadOnlyDictionary<BibleFormat, IBibleFormatReader> _readers;

    public BibleFormatReaderSelector(IEnumerable<IBibleFormatReader> readers)
    {
        _readers = readers.ToDictionary(reader => reader.Format);
    }

    public IBibleFormatReader GetReader(BibleFormat format) =>
        _readers.TryGetValue(format, out var reader)
            ? reader
            : throw new NotSupportedException($"No reader is registered for Bible format '{format}'.");

    public BibleFormat? DetectFormat(Stream? stream)
    {
        if (stream is null || !stream.CanRead || !stream.CanSeek)
        {
            return null;
        }

        var position = stream.Position;
        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            Span<byte> header = stackalloc byte[8];
            var read = stream.Read(header);

            if (read >= 2 && header[0] == 0x1F && header[1] == 0x8B)
            {
                return BibleFormat.Yes;
            }

            if (read >= 2 && header[0] == 0x98 && header[1] == 0x58)
            {
                return BibleFormat.Yes;
            }

            if (read >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
            {
                stream.Seek(0, SeekOrigin.Begin);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                var hasUsfm = false;
                var hasSword = false;
                foreach (var entry in archive.Entries)
                {
                    hasUsfm |= IsUsfm(entry.FullName);
                    hasSword |= IsSword(entry.FullName);
                }

                if (hasUsfm)
                {
                    return BibleFormat.Usfm;
                }

                return hasSword ? BibleFormat.Sword : null;
            }

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var sample = new char[512];
            var count = reader.Read(sample, 0, sample.Length);
            var text = new string(sample, 0, count).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            return text.StartsWith("\\id ", StringComparison.Ordinal) ? BibleFormat.Usfm : null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
        finally
        {
            stream.Position = position;
        }
    }

    private static bool IsUsfm(string name) =>
        name.EndsWith(".usfm", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase);

    private static bool IsSword(string name) =>
        name.Contains("mods.d/", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".conf", StringComparison.OrdinalIgnoreCase);
}
