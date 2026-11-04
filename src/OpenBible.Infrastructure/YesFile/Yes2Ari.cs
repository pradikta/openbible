using OpenBible.Domain.Enums;
using OpenBible.Domain.Exceptions;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.YesFile;

/// <summary>
/// Converts the ARI and ARIF integers stored in YES2 files into domain references.
/// ARIF packs a one-byte entry index into the low bits of an ARI: <c>arif = (ari &lt;&lt; 8) | index</c>.
/// </summary>
internal static class Yes2Ari
{
    public const int BookCount = 66;

    /// <summary>
    /// Unpacks a 24-bit YES2 ARI into a validated <see cref="VerseReference"/>.
    /// </summary>
    public static VerseReference ToReference(int ari)
    {
        var bookId = (ari >> 16) & 0xff;
        var chapter = (ari >> 8) & 0xff;
        var verse = ari & 0xff;
        if (ari < 0 || bookId >= BookCount || chapter < 1)
        {
            throw new YesFileCorruptException($"Invalid YES2 ARI: 0x{ari:X8}.");
        }

        return new VerseReference((BookId)bookId, chapter, verse);
    }

    /// <summary>
    /// Splits an ARIF into its <see cref="VerseReference"/> and one-based entry index.
    /// </summary>
    public static (VerseReference Reference, int Index) SplitArif(int arif)
    {
        if (arif < 0)
        {
            throw new YesFileCorruptException($"Invalid YES2 ARIF: 0x{arif:X8}.");
        }

        var index = arif & 0xff;
        if (index == 0)
        {
            throw new YesFileCorruptException($"YES2 ARIF entry indices start at 1: 0x{arif:X8}.");
        }

        return (ToReference((int)((uint)arif >> 8)), index);
    }
}
