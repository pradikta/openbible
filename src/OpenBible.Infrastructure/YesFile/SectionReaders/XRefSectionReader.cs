namespace OpenBible.Infrastructure.YesFile.SectionReaders;

internal sealed class XRefSectionReader : IYesSectionReader
{
    public string SectionName => YesFileConstants.Sections.CrossReferences;

    public Task ReadSectionAsync(Stream sectionStream, CancellationToken ct = default) =>
        Task.CompletedTask;
}
