namespace OpenBible.Infrastructure.YesFile;

public static class YesFileConstants
{
    /// <summary>
    /// Initial magic bytes identifying a YES binary file: 0x98 0x58.
    /// </summary>
    public static readonly byte[] MagicBytes = [0x98, 0x58];

    /// <summary>Complete eight-byte YES2 signature.</summary>
    public static readonly byte[] Yes2Header = [0x98, 0x58, 0x0d, 0x0a, 0x00, 0x5d, 0xe0, 0x02];

    /// <summary>
    /// Header size in bytes.
    /// </summary>
    public const int HeaderSizeBytes = 8;

    /// <summary>
    /// Section name strings used in YES2 binary files.
    /// </summary>
    public static class Sections
    {
        public const string VersionInfo = "versionInfo";
        public const string BookNames = "booksInfo";
        public const string VerseText = "text";
        public const string Pericope = "pericopes";
        public const string CrossReferences = "xrefs";
        public const string Footnotes = "footnotes";
    }
}
