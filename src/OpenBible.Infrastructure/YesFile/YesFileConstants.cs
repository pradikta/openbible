namespace OpenBible.Infrastructure.YesFile;

public static class YesFileConstants
{
    /// <summary>
    /// Initial magic bytes identifying a YES binary file: 0x98 0x58.
    /// </summary>
    public static readonly byte[] MagicBytes = [0x98, 0x58];

    /// <summary>
    /// Header size in bytes.
    /// </summary>
    public const int HeaderSizeBytes = 8;

    /// <summary>
    /// Section name strings used in YES2 binary files.
    /// </summary>
    public static class Sections
    {
        public const string VersionInfo = "version_info";
        public const string BookNames = "book_names";
        public const string VerseText = "verse_text";
        public const string Pericope = "pericope";
        public const string CrossReferences = "xref";
        public const string Footnotes = "footnote";
    }
}
