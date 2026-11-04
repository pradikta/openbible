using System.Buffers.Binary;
using System.Text;
using OpenBible.Domain.Exceptions;

namespace OpenBible.Infrastructure.YesFile;

/// <summary>Reads the YES2 section directory and exposes validated section boundaries.</summary>
public sealed class Yes2SectionIndex
{
    private const int FixedHeaderLength = 12;
    private const int SectionRecordTailLength = 16;
    private const int MaximumSectionCount = 64;
    private const int MaximumNameLength = 255;
    private readonly Dictionary<string, Yes2SectionEntry> _sections;

    private Yes2SectionIndex(long dataStartOffset, Dictionary<string, Yes2SectionEntry> sections)
    {
        DataStartOffset = dataStartOffset;
        _sections = sections;
    }

    public long DataStartOffset { get; }

    public IReadOnlyCollection<Yes2SectionEntry> Sections => _sections.Values;

    public bool TryGetSection(string name, out Yes2SectionEntry? section) => _sections.TryGetValue(name, out section);

    public static Yes2SectionIndex Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("YES2 section indexes require a readable, seekable stream.", nameof(stream));
        }

        stream.Position = 0;
        Span<byte> signature = stackalloc byte[8];
        ReadExactly(stream, signature);
        if (!signature.SequenceEqual(YesFileConstants.Yes2Header))
        {
            throw new YesFileCorruptException("Invalid YES2 signature.");
        }

        Span<byte> indexSizeBytes = stackalloc byte[4];
        ReadExactly(stream, indexSizeBytes);
        var indexSize = BinaryPrimitives.ReadInt32BigEndian(indexSizeBytes);
        if (indexSize < 5 || indexSize > stream.Length - FixedHeaderLength)
        {
            throw new YesFileCorruptException("Invalid YES2 section index size.");
        }

        var indexEnd = checked((long)FixedHeaderLength + indexSize);
        var version = ReadByte(stream);
        if (version != 1)
        {
            throw new YesFileCorruptException($"Unsupported YES2 section index version: {version}.");
        }

        var sectionCount = ReadInt32(stream);
        if (sectionCount < 0 || sectionCount > MaximumSectionCount)
        {
            throw new YesFileCorruptException("Invalid YES2 section count.");
        }

        var sections = new Dictionary<string, Yes2SectionEntry>(StringComparer.Ordinal);
        for (var i = 0; i < sectionCount; i++)
        {
            var nameLength = ReadByte(stream);
            if (nameLength == 0 || nameLength > MaximumNameLength)
            {
                throw new YesFileCorruptException("Invalid YES2 section name length.");
            }

            var name = Encoding.ASCII.GetString(ReadBytes(stream, nameLength));
            var offset = ReadInt32(stream);
            var attributesSize = ReadInt32(stream);
            var contentSize = ReadInt32(stream);
            _ = ReadBytes(stream, 4); // reserved

            if (offset < 0 || attributesSize < 0 || contentSize < 0)
            {
                throw new YesFileCorruptException($"Invalid bounds for YES2 section '{name}'.");
            }

            var sectionStart = checked(indexEnd + offset);
            var sectionLength = checked((long)attributesSize + contentSize);
            if (sectionStart < indexEnd || sectionStart > stream.Length || sectionLength > stream.Length - sectionStart)
            {
                throw new YesFileCorruptException($"YES2 section '{name}' extends beyond the file boundary.");
            }

            if (!sections.TryAdd(name, new Yes2SectionEntry(name, sectionStart, attributesSize, contentSize)))
            {
                throw new YesFileCorruptException($"Duplicate YES2 section '{name}'.");
            }
        }

        if (stream.Position != indexEnd)
        {
            throw new YesFileCorruptException("YES2 section index size does not match its records.");
        }

        return new Yes2SectionIndex(indexEnd, sections);
    }

    private static byte ReadByte(Stream stream)
    {
        var value = stream.ReadByte();
        return value < 0 ? throw new EndOfStreamException("Unexpected end of YES2 file.") : (byte)value;
    }

    private static int ReadInt32(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[4];
        ReadExactly(stream, bytes);
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }

    private static byte[] ReadBytes(Stream stream, int count)
    {
        var bytes = new byte[count];
        ReadExactly(stream, bytes);
        return bytes;
    }

    private static void ReadExactly(Stream stream, Span<byte> bytes)
    {
        var total = 0;
        while (total < bytes.Length)
        {
            var read = stream.Read(bytes[total..]);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of YES2 file.");
            }

            total += read;
        }
    }
}
