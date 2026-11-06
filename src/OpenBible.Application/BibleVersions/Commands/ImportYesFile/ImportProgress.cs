namespace OpenBible.Application.BibleVersions.Commands.ImportYesFile;

public sealed record ImportProgress(ImportStage Stage, int ProcessedVerses, int TotalVerses)
{
    public double Fraction => TotalVerses > 0
        ? Math.Clamp((double)ProcessedVerses / TotalVerses, 0d, 1d)
        : 0d;
}
