using OpenBible.Domain.Exceptions;

namespace OpenBible.Infrastructure.YesFile.Compression;

/// <summary>
/// Seekable, read-only view over a YES2 <c>snappy-blocks</c> container.
/// The stream coordinate space is the concatenation of the decompressed blocks,
/// which is exactly the space addressed by <c>booksInfo</c> chapter offsets.
/// </summary>
internal sealed class SnappyBlockStream : Stream
{
    private readonly Stream _source;
    private readonly long _contentOffset;
    private readonly bool _leaveOpen;
    private readonly int _blockSize;
    private readonly int[] _blockSizes;
    private readonly long[] _blockOffsets;
    private readonly byte[] _uncompressedBuffer;
    private byte[] _compressedBuffer;
    private int _cachedBlockIndex = -1;
    private int _cachedBlockLength;
    private long _position;

    public SnappyBlockStream(Stream source, long contentOffset, int blockSize, int[] blockSizes, bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(blockSizes);
        if (!source.CanRead || !source.CanSeek)
        {
            throw new ArgumentException("snappy-blocks parsing requires a readable, seekable stream.", nameof(source));
        }

        if (blockSize < 1 || blockSize > 4 * 1024 * 1024)
        {
            throw new YesFileCorruptException($"Invalid snappy-blocks block size: {blockSize}.");
        }

        _source = source;
        _contentOffset = contentOffset;
        _leaveOpen = leaveOpen;
        _blockSize = blockSize;
        _blockSizes = blockSizes;
        _blockOffsets = new long[blockSizes.Length];

        long offset = 0;
        var maximumBlockSize = 0;
        for (var index = 0; index < blockSizes.Length; index++)
        {
            if (blockSizes[index] < 0)
            {
                throw new YesFileCorruptException("snappy-blocks contains a negative block size.");
            }

            _blockOffsets[index] = offset;
            offset = checked(offset + blockSizes[index]);
            maximumBlockSize = Math.Max(maximumBlockSize, blockSizes[index]);
        }

        _compressedBuffer = new byte[maximumBlockSize];
        _uncompressedBuffer = new byte[blockSize];
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    /// <summary>
    /// Nominal container length. The final block may decompress to fewer bytes,
    /// in which case reads stop at the real end of data.
    /// </summary>
    public override long Length => (long)_blockSizes.Length * _blockSize;

    public override long Position
    {
        get => _position;
        set => SeekTo(value);
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            if (_position >= Length)
            {
                break;
            }

            var blockIndex = (int)(_position / _blockSize);
            if (blockIndex >= _blockSizes.Length)
            {
                break;
            }

            EnsureBlock(blockIndex);
            var blockSkip = (int)(_position % _blockSize);
            var available = _cachedBlockLength - blockSkip;
            if (available <= 0)
            {
                // The final block decompressed to fewer bytes than the nominal block size.
                break;
            }

            var copy = Math.Min(available, buffer.Length - total);
            _uncompressedBuffer.AsSpan(blockSkip, copy).CopyTo(buffer[total..]);
            total += copy;
            _position += copy;
        }

        return total;
    }

    public override int ReadByte()
    {
        Span<byte> single = stackalloc byte[1];
        return Read(single) == 1 ? single[0] : -1;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unsupported seek origin.")
        };

        SeekTo(target);
        return _position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException("snappy-blocks streams are read-only.");

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("snappy-blocks streams are read-only.");

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
        {
            _source.Dispose();
        }

        _cachedBlockIndex = -1;
        base.Dispose(disposing);
    }

    private void SeekTo(long position)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Cannot seek before the start of the container.");
        }

        if (position > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Cannot seek beyond the nominal container length.");
        }

        _position = position;
    }

    private void EnsureBlock(int blockIndex)
    {
        if (_cachedBlockIndex == blockIndex)
        {
            return;
        }

        var compressedSize = _blockSizes[blockIndex];
        if (compressedSize == 0)
        {
            _cachedBlockIndex = blockIndex;
            _cachedBlockLength = 0;
            return;
        }

        if (_compressedBuffer.Length < compressedSize)
        {
            _compressedBuffer = new byte[compressedSize];
        }

        _source.Position = checked(_contentOffset + _blockOffsets[blockIndex]);
        ReadExactly(_source, _compressedBuffer, compressedSize);
        var compressed = _compressedBuffer.AsSpan(0, compressedSize);
        var expectedLength = SnappyBlockDecoder.ReadUncompressedLength(compressed, out var preambleLength);
        if (expectedLength > _uncompressedBuffer.Length)
        {
            throw new YesFileCorruptException("snappy-blocks block exceeds its declared block size.");
        }

        _cachedBlockLength = SnappyBlockDecoder.DecodeBlock(compressed[preambleLength..], _uncompressedBuffer);
        if (_cachedBlockLength != expectedLength)
        {
            throw new YesFileCorruptException("snappy-blocks block length does not match its preamble.");
        }

        _cachedBlockIndex = blockIndex;
    }

    private static void ReadExactly(Stream stream, byte[] buffer, int count)
    {
        var read = 0;
        while (read < count)
        {
            var chunk = stream.Read(buffer, read, count - read);
            if (chunk == 0)
            {
                throw new YesFileCorruptException("snappy-blocks data is truncated.");
            }

            read += chunk;
        }
    }
}
