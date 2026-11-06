using OpenBible.Domain.Exceptions;

namespace OpenBible.Infrastructure.YesFile.Sections;

internal static class MetadataSectionReader
{
    public static YesFileMetadata Read(Stream stream, Yes2SectionIndex index)
    {
        if (!index.TryGetSection(YesFileConstants.Sections.VersionInfo, out var section) || section is null)
        {
            throw new YesFileCorruptException("YES2 file does not contain versionInfo metadata.");
        }

        stream.Position = section.ContentOffset;
        var values = BintexValueMapReader.Read(stream, section.ContentSize);

        return new YesFileMetadata(
            BintexValueAccess.GetString(values, "shortName"),
            BintexValueAccess.GetString(values, "longName"),
            BintexValueAccess.GetString(values, "locale") ?? "en",
            BintexValueAccess.GetString(values, "description") ?? string.Empty,
            BintexValueAccess.GetInt(values, "textEncoding", 2),
            BintexValueAccess.GetInt(values, "book_count", -1));
    }
}
