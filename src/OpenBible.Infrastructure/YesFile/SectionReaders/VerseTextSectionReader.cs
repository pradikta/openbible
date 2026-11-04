namespace OpenBible.Infrastructure.YesFile.SectionReaders;

internal sealed class VerseTextSectionReader : IYesSectionReader
{
    public string SectionName => YesFileConstants.Sections.VerseText;

    public Task ReadSectionAsync(Stream sectionStream, CancellationToken ct = default) =>
        Task.CompletedTask;
}
