using OpenBible.Domain.Exceptions;
using OpenBible.Infrastructure.YesFile.Compression;

namespace OpenBible.Infrastructure.YesFile.Sections;

/// <summary>
/// Random access to the payload of a YES2 section. Offsets are relative to the start of
/// the section content and <c>snappy-blocks</c> containers are decompressed transparently.
/// </summary>
internal sealed class SectionContentStream : IDisposable
{
    private const string SnappyBlocksCompression = "snappy-blocks";
    private const int SupportedCompressionVersion = 1;

    private readonly Stream _stream;
    private readonly long _baseOffset;
    private readonly bool _ownsStream;

    private SectionContentStream(Stream stream, long baseOffset, long length, bool ownsStream)
    {
        _stream = stream;
        _baseOffset = baseOffset;
        Length = length;
        _ownsStream = ownsStream;
    }

    /// <summary>
    /// Gets the logical length of the section content in this stream's coordinate space.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets the underlying content stream, positioned relative to the section content start.
    /// </summary>
    public Stream Stream => _stream;

    /// <summary>
    /// Gets the current offset relative to the start of the section content.
    /// </summary>
    public long Position => _stream.Position - _baseOffset;

    /// <summary>
    /// Opens the content of <paramref name="section"/>, decompressing it when the section
    /// attributes declare a supported compression.
    /// </summary>
    public static SectionContentStream Open(Stream file, Yes2SectionEntry section)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(section);

        var attributes = ReadAttributes(file, section);
        var compressionName = attributes is null
            ? null
            : BintexValueAccess.GetString(attributes, "compression.name");

        if (compressionName is null)
        {
            file.Position = section.ContentOffset;
            return new SectionContentStream(file, section.ContentOffset, section.ContentSize, ownsStream: false);
        }

        if (!string.Equals(compressionName, SnappyBlocksCompression, StringComparison.Ordinal))
        {
            throw new NotSupportedException($"YES2 section compression '{compressionName}' is not supported.");
        }

        var compressionVersion = BintexValueAccess.GetInt(attributes!, "compression.version", SupportedCompressionVersion);
        if (compressionVersion > SupportedCompressionVersion)
        {
            throw new NotSupportedException($"YES2 snappy-blocks version {compressionVersion} is not supported.");
        }

        var info = BintexValueAccess.GetMap(attributes!, "compression.info")
            ?? throw new YesFileCorruptException("YES2 snappy-blocks section is missing compression.info.");

        var blockSize = BintexValueAccess.GetInt(info, "block_size", -1);
        var blockSizes = BintexValueAccess.GetIntArray(info, "compressed_block_sizes");
        var snappy = new SnappyBlockStream(file, section.ContentOffset, blockSize, blockSizes);
        return new SectionContentStream(snappy, 0, snappy.Length, ownsStream: true);
    }

    /// <summary>
    /// Positions the content stream at <paramref name="relativeOffset"/> from the content start.
    /// </summary>
    public void SeekTo(long relativeOffset)
    {
        if (relativeOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeOffset), relativeOffset, "Offset precedes the section content.");
        }

        _stream.Position = checked(_baseOffset + relativeOffset);
    }

    public void Dispose()
    {
        if (_ownsStream)
        {
            _stream.Dispose();
        }
    }

    private static IReadOnlyDictionary<string, object?>? ReadAttributes(Stream file, Yes2SectionEntry section)
    {
        if (section.AttributesSize == 0)
        {
            return null;
        }

        if (section.AttributesSize < 0)
        {
            throw new YesFileCorruptException("YES2 section declares a negative attributes size.");
        }

        file.Position = section.StartOffset;
        return BintexValueMapReader.Read(file, section.AttributesSize);
    }
}
