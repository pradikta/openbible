using OpenBible.Domain.Entities;
using OpenBible.Domain.Exceptions;

namespace OpenBible.Infrastructure.YesFile.Sections;

/// <summary>
/// Reads the YES2 <c>pericopes</c> section: a packed index of <c>ari + offset</c> entries
/// followed by variable-length pericope blocks (layout ported from AlkitabYes2).
/// </summary>
internal static class PericopeSectionReader
{
    private const int MaximumEntryCount = 200_000;
    private const int SupportedBlockVersion = 4;

    public static IReadOnlyList<Pericope> Read(SectionContentStream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        content.SeekTo(0);
        using var reader = new BigEndianBinaryReader(content.Stream, leaveOpen: true);
        var dataFormatVersion = reader.ReadByte();
        if (dataFormatVersion is not (2 or 3))
        {
            throw new NotSupportedException($"YES2 pericopes data format version {dataFormatVersion} is not supported.");
        }

        _ = reader.ReadInt32(); // Total index size in bytes; derived values are authoritative.
        var entryCount = reader.ReadInt32();
        if (entryCount < 0 || entryCount > MaximumEntryCount)
        {
            throw new YesFileCorruptException($"Invalid YES2 pericope entry count: {entryCount}.");
        }

        var aris = new int[entryCount];
        var offsets = new int[entryCount];
        ReadIndex(reader, dataFormatVersion, aris, offsets);

        var dataStart = content.Position;
        var pericopes = new List<Pericope>(entryCount);
        for (var index = 0; index < entryCount; index++)
        {
            if (offsets[index] < 0 || offsets[index] >= content.Length - dataStart)
            {
                throw new YesFileCorruptException("YES2 pericope offset is outside the pericopes section.");
            }

            content.SeekTo(dataStart + offsets[index]);
            var block = ReadBlock(reader);
            if (block is not null)
            {
                pericopes.Add(new Pericope(Yes2Ari.ToReference(aris[index]), block.Title, block.ParallelPassages));
            }
        }

        return pericopes;
    }

    private static void ReadIndex(BigEndianBinaryReader reader, int dataFormatVersion, int[] aris, int[] offsets)
    {
        if (dataFormatVersion == 2)
        {
            for (var index = 0; index < aris.Length; index++)
            {
                aris[index] = reader.ReadInt32();
                offsets[index] = reader.ReadInt32();
            }

            return;
        }

        // Version 3 stores the first ARI absolutely and the rest as deltas, which keeps the
        // index compact for large translations.
        var previousAri = 0;
        var previousOffset = 0;
        for (var index = 0; index < aris.Length; index++)
        {
            var ariHigh = (int)reader.ReadUInt16();
            if ((ariHigh & 0x8000) == 0)
            {
                previousAri = (ariHigh << 16) | reader.ReadUInt16();
            }
            else
            {
                previousAri += ariHigh & 0x7fff;
            }

            aris[index] = previousAri;
            previousOffset += reader.ReadUInt16();
            offsets[index] = previousOffset;
        }
    }

    private static PericopeBlock? ReadBlock(BigEndianBinaryReader reader)
    {
        var blockVersion = reader.ReadByte();
        if (blockVersion != SupportedBlockVersion)
        {
            throw new NotSupportedException($"YES2 pericope block version {blockVersion} is not supported.");
        }

        var title = BintexValueMapReader.ReadValueString(reader.BaseStream);
        var parallelCount = reader.ReadByte();
        var parallels = parallelCount == 0 ? [] : new string[parallelCount];
        for (var index = 0; index < parallels.Length; index++)
        {
            parallels[index] = BintexValueMapReader.ReadValueString(reader.BaseStream);
        }

        // Headings without a title carry no information for the reader UI.
        return string.IsNullOrWhiteSpace(title) ? null : new PericopeBlock(title, parallels);
    }

    private sealed record PericopeBlock(string Title, IReadOnlyList<string> ParallelPassages);
}
