using System.Runtime.CompilerServices;
using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Commands.ImportYesFile;

public sealed class ImportYesFileHandler : IRequestHandler<ImportYesFileCommand, Result<BibleVersionDto>>
{
    private readonly IBibleFormatReaderSelector _formatReaders;
    private readonly IBibleRepository _bibleRepository;
    private readonly ISearchIndex? _searchIndex;

    public ImportYesFileHandler(
        IBibleFormatReaderSelector formatReaders,
        IBibleRepository bibleRepository,
        ISearchIndex? searchIndex = null)
    {
        _formatReaders = formatReaders;
        _bibleRepository = bibleRepository;
        _searchIndex = searchIndex;
    }

    public async Task<Result<BibleVersionDto>> Handle(ImportYesFileCommand request, CancellationToken ct)
    {
        if (request.FileStream is null || !request.FileStream.CanRead)
        {
            return Result<BibleVersionDto>.Failure("Input stream is null or unreadable.");
        }

        var progress = request.Progress;

        try
        {
            var reader = _formatReaders.GetReader(request.Format);

            progress?.Report(new ImportProgress(ImportStage.ReadingMetadata, 0, 0));
            var metadata = await reader.ReadMetadataAsync(request.FileStream, ct).ConfigureAwait(false);
            var version = new BibleVersion(
                metadata.Id,
                metadata.Title,
                metadata.Language,
                metadata.Description,
                metadata.Books,
                request.Format,
                request.SourceId,
                request.License ?? metadata.License);
            var totalVerses = version.Books.Sum(book => book.VerseCounts.Sum());

            progress?.Report(new ImportProgress(ImportStage.SavingVerses, 0, totalVerses));
            var verses = ReportProgress(
                reader.ReadVersesAsync(request.FileStream, ct),
                progress,
                ImportStage.SavingVerses,
                totalVerses);
            var pericopes = reader.ReadPericopesAsync(request.FileStream, ct);

            await _bibleRepository.SaveVersionAsync(version, verses, pericopes, ct).ConfigureAwait(false);

            if (_searchIndex is not null)
            {
                // Re-read verses for search index if stream is seekable
                if (request.FileStream.CanSeek)
                {
                    request.FileStream.Seek(0, SeekOrigin.Begin);
                    progress?.Report(new ImportProgress(ImportStage.IndexingVerses, 0, totalVerses));
                    var indexVerses = ReportProgress(
                        reader.ReadVersesAsync(request.FileStream, ct),
                        progress,
                        ImportStage.IndexingVerses,
                        totalVerses);
                    await _searchIndex.IndexVersionAsync(version.Id, indexVerses, ct).ConfigureAwait(false);
                }
            }

            progress?.Report(new ImportProgress(ImportStage.Completed, totalVerses, totalVerses));

            var dto = new BibleVersionDto(
                version.Id,
                version.Title,
                version.Language,
                version.Description,
                version.Books.Count,
                version.Format,
                version.SourceId,
                version.License
            );

            return Result<BibleVersionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<BibleVersionDto>.Failure($"Failed to import translation: {ex.Message}");
        }
    }

    private static async IAsyncEnumerable<Verse> ReportProgress(
        IAsyncEnumerable<Verse> verses,
        IProgress<ImportProgress>? progress,
        ImportStage stage,
        int totalVerses,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var step = totalVerses > 0 ? Math.Max(1, totalVerses / 100) : 500;
        var processed = 0;

        await foreach (var verse in verses.WithCancellation(ct).ConfigureAwait(false))
        {
            processed++;

            if (progress is not null && processed % step == 0)
            {
                progress.Report(new ImportProgress(stage, processed, totalVerses));
            }

            yield return verse;
        }

        progress?.Report(new ImportProgress(stage, processed, totalVerses));
    }
}
