namespace OpenBible.Infrastructure.YesFile.SectionReaders;

internal sealed class MetadataSectionReader : IYesSectionReader
{
    public string SectionName => YesFileConstants.Sections.VersionInfo;

    public Task ReadSectionAsync(Stream sectionStream, CancellationToken ct = default) =>
        Task.CompletedTask;
}
