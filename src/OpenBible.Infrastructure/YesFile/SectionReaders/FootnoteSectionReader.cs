namespace OpenBible.Infrastructure.YesFile.SectionReaders;

internal sealed class FootnoteSectionReader : IYesSectionReader
{
    public string SectionName => YesFileConstants.Sections.Footnotes;

    public Task ReadSectionAsync(Stream sectionStream, CancellationToken ct = default) =>
        Task.CompletedTask;
}
