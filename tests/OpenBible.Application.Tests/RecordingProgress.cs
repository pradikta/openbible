using OpenBible.Application.BibleVersions.Commands.ImportYesFile;

namespace OpenBible.Application.Tests;

internal sealed class RecordingProgress : IProgress<ImportProgress>
{
    public List<ImportProgress> Reports { get; } = new();

    public void Report(ImportProgress value) => Reports.Add(value);
}
