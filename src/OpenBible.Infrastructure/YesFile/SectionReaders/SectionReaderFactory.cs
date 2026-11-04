namespace OpenBible.Infrastructure.YesFile.SectionReaders;

internal sealed class SectionReaderFactory
{
    public static IYesSectionReader Create(string sectionName) => sectionName switch
    {
        YesFileConstants.Sections.VersionInfo => new MetadataSectionReader(),
        YesFileConstants.Sections.VerseText => new VerseTextSectionReader(),
        YesFileConstants.Sections.Pericope => new PericopeSectionReader(),
        YesFileConstants.Sections.CrossReferences => new XRefSectionReader(),
        YesFileConstants.Sections.Footnotes => new FootnoteSectionReader(),
        _ => throw new ArgumentException($"Invalid section name: {sectionName}", nameof(sectionName))
    };
}
