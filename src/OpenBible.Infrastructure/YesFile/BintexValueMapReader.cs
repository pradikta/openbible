using System.Buffers.Binary;
using System.Text;
using OpenBible.Domain.Exceptions;

namespace OpenBible.Infrastructure.YesFile;

internal static class BintexValueMapReader
{
    private const int MaximumStringBytes = 1_048_576;

    public static IReadOnlyDictionary<string, object?> Read(Stream stream, int contentLength)
    {
        if (contentLength < 1 || contentLength > MaximumStringBytes)
        {
            throw new YesFileCorruptException("Invalid Bintex map length.");
        }

        var end = checked(stream.Position + contentLength);
        var values = ReadMap(stream);
        if (stream.Position != end)
        {
            throw new YesFileCorruptException("Bintex map content length does not match its values.");
        }

        return values;
    }

    public static IReadOnlyDictionary<string, object?> ReadMap(Stream stream) => ReadMap(stream, ReadByte(stream));

    private static IReadOnlyDictionary<string, object?> ReadMap(Stream stream, byte tag)
    {
        var count = tag switch
        {
            0x90 => 0,
            0x91 => ReadByte(stream),
            _ => throw new YesFileCorruptException($"Expected a Bintex Simple Map, got tag 0x{tag:X2}.")
        };

        var values = new Dictionary<string, object?>(count, StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var keyLength = ReadByte(stream);
            var key = Encoding.ASCII.GetString(ReadBytes(stream, keyLength));
            if (!values.TryAdd(key, ReadValue(stream)))
            {
                throw new YesFileCorruptException($"Duplicate Bintex map key '{key}'.");
            }
        }

        return values;
    }

    /// <summary>
    /// Reads a single Bintex value and requires it to be a string.
    /// </summary>
    public static string ReadValueString(Stream stream)
    {
        var value = ReadValue(stream);
        return value switch
        {
            string text => text,
            null => string.Empty,
            _ => throw new YesFileCorruptException("Expected a Bintex string value.")
        };
    }

    private static object? ReadValue(Stream stream)
    {
        var tag = ReadByte(stream);
        if (tag is >= 0x01 and <= 0x07) return (int)tag;
        if (tag == 0x0c) return null;
        if (tag == 0x0d) return string.Empty;
        if (tag == 0x0e) return 0;
        if (tag == 0x0f) return -1;
        if (tag is >= 0x51 and <= 0x5f) return Encoding.Latin1.GetString(ReadBytes(stream, tag & 0x0f));
        if (tag is >= 0x61 and <= 0x6f) return ReadUtf16(stream, tag & 0x0f);

        return tag switch
        {
            0x10 => (int)ReadByte(stream),
            0x11 => ~ReadByte(stream),
            0x20 => (int)ReadUInt16(stream),
            0x21 => ~ReadUInt16(stream),
            0x30 => ReadUInt24(stream),
            0x31 => ~ReadUInt24(stream),
            0x40 => ReadInt32(stream),
            0x41 => ~ReadInt32(stream),
            0x70 => ReadLatin1(stream, ReadByte(stream)),
            0x71 => ReadUtf16(stream, ReadByte(stream)),
            0x72 => ReadLatin1(stream, ReadLength(stream)),
            0x73 => ReadUtf16(stream, ReadLength(stream)),
            0xc0 => ReadArray(stream, ReadByte(stream), 1),
            0xc1 => ReadArray(stream, ReadByte(stream), 2),
            0xc4 => ReadArray(stream, ReadByte(stream), 4),
            0xc8 => ReadArray(stream, ReadLength(stream), 1),
            0xc9 => ReadArray(stream, ReadLength(stream), 2),
            0xcc => ReadArray(stream, ReadLength(stream), 4),
            0x90 or 0x91 => ReadMap(stream, tag),
            _ => throw new YesFileCorruptException($"Unsupported Bintex value tag 0x{tag:X2}.")
        };
    }

    public static int ReadVarUInt(Stream stream)
    {
        var first = ReadByte(stream);
        if ((first & 0x80) == 0) return first;
        if ((first & 0xc0) == 0x80) return ((first & 0x3f) << 8) | ReadByte(stream);
        if ((first & 0xe0) == 0xc0) return ((first & 0x1f) << 16) | (ReadByte(stream) << 8) | ReadByte(stream);
        if ((first & 0xf0) == 0xe0) return ((first & 0x0f) << 24) | (ReadByte(stream) << 16) | (ReadByte(stream) << 8) | ReadByte(stream);
        if (first == 0xf0) return ReadInt32(stream);
        throw new YesFileCorruptException("Invalid Bintex VarUInt prefix.");
    }

    private static int[] ReadArray(Stream stream, int count, int elementSize)
    {
        if (count < 0 || (long)count * elementSize > MaximumStringBytes)
        {
            throw new YesFileCorruptException("Bintex integer array exceeds the supported length.");
        }

        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = elementSize switch
            {
                1 => ReadByte(stream),
                2 => ReadUInt16(stream),
                _ => ReadInt32(stream)
            };
        }

        return result;
    }

    private static string ReadLatin1(Stream stream, int length)
    {
        ValidateLength(length, 1);
        return Encoding.Latin1.GetString(ReadBytes(stream, length));
    }

    private static string ReadUtf16(Stream stream, int characterCount)
    {
        ValidateLength(characterCount, 2);
        var bytes = ReadBytes(stream, characterCount * 2);
        for (var i = 0; i < bytes.Length; i += 2)
        {
            (bytes[i], bytes[i + 1]) = (bytes[i + 1], bytes[i]);
        }

        return Encoding.Unicode.GetString(bytes);
    }

    private static void ValidateLength(int length, int bytesPerCharacter)
    {
        if (length < 0 || (long)length * bytesPerCharacter > MaximumStringBytes)
        {
            throw new YesFileCorruptException("Bintex string exceeds the supported length.");
        }
    }

    private static int ReadLength(Stream stream)
    {
        var length = ReadInt32(stream);
        ValidateLength(length, 1);
        return length;
    }

    private static byte ReadByte(Stream stream)
    {
        var value = stream.ReadByte();
        return value < 0 ? throw new EndOfStreamException("Unexpected end of Bintex value.") : (byte)value;
    }

    private static ushort ReadUInt16(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[2];
        ReadExactly(stream, buffer);
        return BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }

    private static int ReadUInt24(Stream stream) => (ReadByte(stream) << 16) | (ReadByte(stream) << 8) | ReadByte(stream);

    private static int ReadInt32(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        ReadExactly(stream, buffer);
        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }

    private static byte[] ReadBytes(Stream stream, int length)
    {
        var bytes = new byte[length];
        ReadExactly(stream, bytes);
        return bytes;
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var count = stream.Read(buffer[read..]);
            if (count == 0) throw new EndOfStreamException("Unexpected end of Bintex value.");
            read += count;
        }
    }
}
