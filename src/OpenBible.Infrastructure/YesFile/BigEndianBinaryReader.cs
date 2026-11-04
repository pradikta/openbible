using System.Buffers.Binary;
using System.Text;

namespace OpenBible.Infrastructure.YesFile;

/// <summary>
/// Binary reader reading big-endian integers and UTF-8 strings conforming to Java DataInputStream format.
/// </summary>
public sealed class BigEndianBinaryReader : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly byte[] _buffer = new byte[8];

    public Stream BaseStream => _stream;

    public BigEndianBinaryReader(Stream stream, bool leaveOpen = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _leaveOpen = leaveOpen;
    }

    public byte ReadByte()
    {
        var b = _stream.ReadByte();
        if (b == -1)
        {
            throw new EndOfStreamException();
        }
        return (byte)b;
    }

    public byte[] ReadBytes(int count)
    {
        var result = new byte[count];
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = _stream.Read(result, totalRead, count - totalRead);
            if (read == 0)
            {
                throw new EndOfStreamException($"Expected {count} bytes, but reached end of stream at {totalRead} bytes.");
            }
            totalRead += read;
        }
        return result;
    }

    public short ReadInt16()
    {
        ReadInternal(2);
        return BinaryPrimitives.ReadInt16BigEndian(_buffer.AsSpan(0, 2));
    }

    public ushort ReadUInt16()
    {
        ReadInternal(2);
        return BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(0, 2));
    }

    public int ReadInt32()
    {
        ReadInternal(4);
        return BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(0, 4));
    }

    public uint ReadUInt32()
    {
        ReadInternal(4);
        return BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(0, 4));
    }

    public long ReadInt64()
    {
        ReadInternal(8);
        return BinaryPrimitives.ReadInt64BigEndian(_buffer.AsSpan(0, 8));
    }

    /// <summary>
    /// Reads a UTF-8 string with a 1-byte length prefix.
    /// </summary>
    public string ReadShortString()
    {
        var length = ReadByte();
        if (length == 0) return string.Empty;
        var bytes = ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Reads a UTF-8 string with a 2-byte (big-endian) length prefix.
    /// </summary>
    public string ReadString()
    {
        var length = ReadUInt16();
        if (length == 0) return string.Empty;
        var bytes = ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private void ReadInternal(int byteCount)
    {
        var totalRead = 0;
        while (totalRead < byteCount)
        {
            var read = _stream.Read(_buffer, totalRead, byteCount - totalRead);
            if (read == 0)
            {
                throw new EndOfStreamException($"Expected {byteCount} bytes, reached end of stream at {totalRead}.");
            }
            totalRead += read;
        }
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}
