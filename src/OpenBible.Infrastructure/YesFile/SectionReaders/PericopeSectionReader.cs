namespace OpenBible.Infrastructure.YesFile.SectionReaders;

internal sealed class PericopeSectionReader : IYesSectionReader
{
    public string SectionName => YesFileConstants.Sections.Pericope;

    public Task ReadSectionAsync(Stream sectionStream, CancellationToken ct = default) =>
        Task.CompletedTask;
}
