using System.Buffers.Binary;
using System.Text;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.YesFile;

namespace OpenBible.Infrastructure.Tests;

/// <summary>
/// Builds minimal synthetic YES2 files so parser tests stay completely offline.
/// </summary>
internal static class Yes2TestFile
{
    public static MemoryStream Create(params (string Name, byte[] Content)[] sections) =>
        CreateWithAttributes(sections.Select(static section => (section.Name, (byte[]?)null, section.Content)).ToArray());

    public static MemoryStream CreateWithAttributes(params (string Name, byte[]? Attributes, byte[] Content)[] sections)
    {
        using var indexBody = new MemoryStream();
        indexBody.WriteByte(1);
        WriteInt32(indexBody, sections.Length);
        var offset = 0;
        foreach (var (name, attributes, content) in sections)
        {
            var nameBytes = Encoding.ASCII.GetBytes(name);
            indexBody.WriteByte((byte)nameBytes.Length);
            indexBody.Write(nameBytes);
            WriteInt32(indexBody, offset);
            WriteInt32(indexBody, attributes?.Length ?? 0);
            WriteInt32(indexBody, content.Length);
            indexBody.Write([0, 0, 0, 0]);
            offset += (attributes?.Length ?? 0) + content.Length;
        }

        var file = new MemoryStream();
        file.Write(YesFileConstants.Yes2Header);
        WriteInt32(file, checked((int)indexBody.Length) - 5); // Real files exclude the version byte and count.
        indexBody.Position = 0;
        indexBody.CopyTo(file);
        foreach (var (_, attributes, content) in sections)
        {
            if (attributes is not null)
            {
                file.Write(attributes);
            }

            file.Write(content);
        }

        file.WriteByte(0); // Documented footer.
        file.Position = 0;
        return file;
    }

    public static byte[] MetadataMap(string shortName = "TST", string longName = "Test Version") =>
        Map(("shortName", EncodeString(shortName)), ("longName", EncodeString(longName)), ("locale", EncodeString("en")));

    public static byte[] GenesisChapterOneBooksInfo(int verseCount = 1) =>
    [
        .. Int32ToBytes(1),
        .. Map(
            ("bookId", EncodeInt(0)),
            ("shortName", EncodeString("Gen")),
            ("offset", EncodeInt(0)),
            ("chapter_count", EncodeInt(1)),
            ("verse_counts", EncodeIntArray([verseCount])),
            ("chapter_offsets", EncodeIntArray([0])))
    ];

    /// <summary>
    /// Builds a <c>text</c> section holding one UTF-8 verse record per entry.
    /// </summary>
    public static byte[] VerseTextSection(params string[] verses)
    {
        using var stream = new MemoryStream();
        foreach (var verse in verses)
        {
            var bytes = Encoding.UTF8.GetBytes(verse);
            WriteVarUInt(stream, bytes.Length);
            stream.Write(bytes);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Builds an ARIF-indexed section (<c>footnotes</c> or <c>xrefs</c>): version byte,
    /// entry count, ARIF keys, relative offsets, then one Bintex value string per entry.
    /// </summary>
    public static byte[] ArifSection(params (int Arif, string Content)[] entries)
    {
        using var content = new MemoryStream();
        var offsets = new int[entries.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            offsets[index] = checked((int)content.Length);
            content.Write(EncodeString(entries[index].Content));
        }

        using var section = new MemoryStream();
        section.WriteByte(1); // data format version
        WriteInt32(section, entries.Length);
        foreach (var entry in entries)
        {
            WriteInt32(section, entry.Arif);
        }

        foreach (var offset in offsets)
        {
            WriteInt32(section, offset);
        }

        content.Position = 0;
        content.CopyTo(section);
        return section.ToArray();
    }

    /// <summary>
    /// Packs a verse reference and a one-based entry index into the ARIF stored in YES2 files.
    /// </summary>
    public static int Arif(BookId book, int chapter, int verse, int index) =>
        (new VerseReference(book, chapter, verse).ToAri() << 8) | index;

    public static void WriteVarUInt(Stream stream, int value)
    {
        if (value < 0x80)
        {
            stream.WriteByte(checked((byte)value));
        }
        else if (value < 0x4000)
        {
            stream.WriteByte(checked((byte)(0x80 | (value >> 8))));
            stream.WriteByte(checked((byte)value));
        }
        else if (value < 0x200000)
        {
            stream.WriteByte(checked((byte)(0xc0 | (value >> 16))));
            stream.WriteByte(checked((byte)(value >> 8)));
            stream.WriteByte(checked((byte)value));
        }
        else
        {
            stream.WriteByte(checked((byte)(0xe0 | (value >> 24))));
            stream.WriteByte(checked((byte)(value >> 16)));
            stream.WriteByte(checked((byte)(value >> 8)));
            stream.WriteByte(checked((byte)value));
        }
    }

    /// <summary>
    /// Builds the <c>text</c> section attributes declaring a <c>snappy-blocks</c> container.
    /// </summary>
    public static byte[] SnappyBlocksAttributes(int blockSize, params int[] compressedBlockSizes) =>
        Map(
            ("compression.name", EncodeString("snappy-blocks")),
            ("compression.version", EncodeInt(1)),
            ("compression.info", Map(
                ("block_size", EncodeInt(blockSize)),
                ("compressed_block_sizes", EncodeIntArray(compressedBlockSizes)))));

    /// <summary>
    /// Encodes <paramref name="payload"/> as one raw Snappy block using a single literal element.
    /// </summary>
    public static byte[] SnappyLiteral(ReadOnlySpan<byte> payload)
    {
        var result = new List<byte>(payload.Length + 8);
        var remaining = payload.Length;
        while (remaining >= 0x80)
        {
            result.Add((byte)((remaining & 0x7f) | 0x80));
            remaining >>= 7;
        }

        result.Add((byte)remaining);
        if (payload.IsEmpty)
        {
            return [.. result];
        }

        if (payload.Length <= 60)
        {
            result.Add((byte)((payload.Length - 1) << 2));
        }
        else
        {
            result.Add(60 << 2);
            result.Add((byte)(payload.Length - 1));
        }

        result.AddRange(payload.ToArray());
        return [.. result];
    }

    public static byte[] Map(params (string Key, byte[] EncodedValue)[] entries)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(0x91);
        stream.WriteByte(checked((byte)entries.Length));
        foreach (var (key, value) in entries)
        {
            var keyBytes = Encoding.ASCII.GetBytes(key);
            stream.WriteByte(checked((byte)keyBytes.Length));
            stream.Write(keyBytes);
            stream.Write(value);
        }

        return stream.ToArray();
    }

    public static byte[] EncodeString(string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        if (bytes.Length == 0)
        {
            return [0x0d];
        }

        if (bytes.Length > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Fixture strings must fit a 15-character Bintex short string.");
        }

        return [.. new[] { checked((byte)(0x50 + bytes.Length)) }, .. bytes];
    }

    public static byte[] EncodeInt(int value) => value switch
    {
        0 => [0x0e],
        >= 1 and <= 7 => [checked((byte)value)],
        >= 8 and <= 255 => [0x10, checked((byte)value)],
        >= 0 and <= 65535 => [0x20, checked((byte)(value >> 8)), checked((byte)value)],
        _ => [0x40, .. Int32ToBytes(value)]
    };

    public static byte[] EncodeIntArray(int[] values)
    {
        if (values.Length == 0)
        {
            return [0xc0, 0];
        }

        if (values.Any(static value => value is < 0 or > 255) || values.Length > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(values), "Fixture arrays support at most 255 uint8 values.");
        }

        return [0xc0, checked((byte)values.Length), .. values.Select(static value => checked((byte)value))];
    }

    public static byte[] Int32ToBytes(int value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        return bytes;
    }

    public static void WriteInt32(Stream stream, int value) => stream.Write(Int32ToBytes(value));
}
