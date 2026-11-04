using OpenBible.Domain.Exceptions;

namespace OpenBible.Infrastructure.YesFile.Sections;

/// <summary>
/// Reads the ARIF-indexed YES2 sections (<c>footnotes</c> and <c>xrefs</c>): a one-byte data
/// format version, an entry count, sorted ARIF keys, entry offsets relative to the end of the
/// index, and then one Bintex value string per entry (layout ported from AlkitabYes2).
/// </summary>
internal static class ArifEntrySectionReader
{
    private const byte SupportedDataFormatVersion = 1;
    private const int MaximumEntryCount = 1_000_000;

    public static IReadOnlyList<ArifEntry> Read(SectionContentStream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        content.SeekTo(0);
        using var reader = new BigEndianBinaryReader(content.Stream, leaveOpen: true);
        var dataFormatVersion = reader.ReadByte();
        if (dataFormatVersion != SupportedDataFormatVersion)
        {
            throw new NotSupportedException($"YES2 ARIF section data format version {dataFormatVersion} is not supported.");
        }

        var entryCount = reader.ReadInt32();
        if (entryCount < 0 || entryCount > MaximumEntryCount)
        {
            throw new YesFileCorruptException($"Invalid YES2 ARIF entry count: {entryCount}.");
        }

        var arifs = new int[entryCount];
        for (var index = 0; index < entryCount; index++)
        {
            arifs[index] = reader.ReadInt32();
        }

        var offsets = new int[entryCount];
        for (var index = 0; index < entryCount; index++)
        {
            offsets[index] = reader.ReadInt32();
        }

        var dataStart = content.Position;
        var dataLength = content.Length - dataStart;
        var entries = new List<ArifEntry>(entryCount);
        for (var index = 0; index < entryCount; index++)
        {
            if (offsets[index] < 0 || offsets[index] >= dataLength)
            {
                throw new YesFileCorruptException("YES2 ARIF entry offset is outside its section.");
            }

            content.SeekTo(dataStart + offsets[index]);
            entries.Add(new ArifEntry(arifs[index], BintexValueMapReader.ReadValueString(content.Stream)));
        }

        return entries;
    }
}
