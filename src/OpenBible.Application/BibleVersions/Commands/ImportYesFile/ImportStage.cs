namespace OpenBible.Application.BibleVersions.Commands.ImportYesFile;

public enum ImportStage
{
    ReadingMetadata,
    SavingVerses,
    IndexingVerses,
    Completed
}
