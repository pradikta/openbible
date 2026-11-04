namespace OpenBible.Infrastructure.YesFile.SectionReaders;

public interface IYesSectionReader
{
    string SectionName { get; }
    Task ReadSectionAsync(Stream sectionStream, CancellationToken ct = default);
}
