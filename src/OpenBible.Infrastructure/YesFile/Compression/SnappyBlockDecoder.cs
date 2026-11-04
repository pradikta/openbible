using OpenBible.Domain.Exceptions;

namespace OpenBible.Infrastructure.YesFile.Compression;

/// <summary>
/// Decodes a single raw Snappy block (the format described by
/// <c>format_description.txt</c> in the upstream Snappy project) into its
/// uncompressed bytes.
/// </summary>
/// <remarks>
/// This implementation is fully managed so it runs in Blazor WebAssembly without a
/// native dependency. YES2 files store each <c>snappy-blocks</c> block as one of
/// these blocks; the block boundaries come from the section attributes.
/// </remarks>
public static class SnappyBlockDecoder
{
    private const int MaximumPreambleBytes = 5;
    private const int MaximumOutputLength = 64 * 1024 * 1024;

    /// <summary>
    /// Reads the uncompressed length stored in the block preamble.
    /// </summary>
    /// <param name="block">Raw Snappy block, including its preamble.</param>
    /// <param name="preambleLength">Number of bytes occupied by the preamble.</param>
    public static int ReadUncompressedLength(ReadOnlySpan<byte> block, out int preambleLength)
    {
        var length = 0;
        for (var index = 0; index < block.Length && index < MaximumPreambleBytes; index++)
        {
            var value = block[index];
            length |= (value & 0x7f) << (index * 7);
            if ((value & 0x80) == 0)
            {
                if (length < 0 || length > MaximumOutputLength)
                {
                    throw new YesFileCorruptException("Snappy block declares an unsupported uncompressed length.");
                }

                preambleLength = index + 1;
                return length;
            }
        }

        throw new YesFileCorruptException("Snappy block preamble does not encode a valid uncompressed length.");
    }

    /// <summary>
    /// Decompresses one raw Snappy block into a new buffer.
    /// </summary>
    public static byte[] Decode(ReadOnlySpan<byte> block)
    {
        var length = ReadUncompressedLength(block, out var preambleLength);
        var output = new byte[length];
        var written = DecodeBlock(block[preambleLength..], output);
        if (written != length)
        {
            throw new YesFileCorruptException("Snappy block content length does not match its preamble.");
        }

        return output;
    }

    /// <summary>
    /// Decompresses the element stream of a Snappy block into <paramref name="destination"/>.
    /// </summary>
    /// <param name="input">Block payload positioned after the uncompressed-length preamble.</param>
    /// <param name="destination">Buffer that receives the uncompressed bytes.</param>
    /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
    public static int DecodeBlock(ReadOnlySpan<byte> input, Span<byte> destination)
    {
        var read = 0;
        var written = 0;
        while (read < input.Length)
        {
            var tag = input[read++];
            switch (tag & 0x03)
            {
                case 0:
                {
                    var length = ReadLiteralLength(input, ref read, tag);
                    if (read + length > input.Length || written + length > destination.Length)
                    {
                        throw new YesFileCorruptException("Snappy literal exceeds the block boundary.");
                    }

                    input.Slice(read, length).CopyTo(destination[written..]);
                    read += length;
                    written += length;
                    break;
                }

                case 1:
                {
                    if (read + 1 > input.Length)
                    {
                        throw new YesFileCorruptException("Snappy copy-1 element is truncated.");
                    }

                    var length = 4 + ((tag >> 2) & 0x07);
                    var offset = ((tag & 0xe0) << 3) | input[read];
                    read += 1;
                    written = CopyBack(offset, length, destination, written);
                    break;
                }

                case 2:
                {
                    if (read + 2 > input.Length)
                    {
                        throw new YesFileCorruptException("Snappy copy-2 element is truncated.");
                    }

                    var length = (tag >> 2) + 1;
                    var offset = input[read] | (input[read + 1] << 8);
                    read += 2;
                    written = CopyBack(offset, length, destination, written);
                    break;
                }

                default:
                {
                    if (read + 4 > input.Length)
                    {
                        throw new YesFileCorruptException("Snappy copy-4 element is truncated.");
                    }

                    var length = (tag >> 2) + 1;
                    var offset = input[read]
                        | (input[read + 1] << 8)
                        | (input[read + 2] << 16)
                        | (input[read + 3] << 24);
                    read += 4;
                    written = CopyBack(offset, length, destination, written);
                    break;
                }
            }
        }

        return written;
    }

    private static int ReadLiteralLength(ReadOnlySpan<byte> input, ref int read, byte tag)
    {
        var lengthCode = tag >> 2;
        if (lengthCode < 60)
        {
            return lengthCode + 1;
        }

        var extraBytes = lengthCode - 59;
        if (extraBytes > 4 || read + extraBytes > input.Length)
        {
            throw new YesFileCorruptException("Snappy literal length is truncated.");
        }

        var length = 0;
        for (var index = 0; index < extraBytes; index++)
        {
            length |= input[read + index] << (index * 8);
        }

        read += extraBytes;
        if (length < 0)
        {
            throw new YesFileCorruptException("Snappy literal length overflows the supported range.");
        }

        return length + 1;
    }

    private static int CopyBack(int offset, int length, Span<byte> destination, int written)
    {
        if (offset <= 0 || offset > written || written + length > destination.Length)
        {
            throw new YesFileCorruptException("Snappy copy element references data outside the decoded block.");
        }

        var source = written - offset;
        for (var index = 0; index < length; index++)
        {
            destination[written + index] = destination[source + index];
        }

        return written + length;
    }
}
