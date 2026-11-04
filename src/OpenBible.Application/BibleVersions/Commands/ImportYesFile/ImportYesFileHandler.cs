using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Commands.ImportYesFile;

public sealed class ImportYesFileHandler : IRequestHandler<ImportYesFileCommand, Result<BibleVersionDto>>
{
    private readonly IYesFileReader _yesFileReader;
    private readonly IBibleRepository _bibleRepository;
    private readonly ISearchIndex? _searchIndex;

    public ImportYesFileHandler(
        IYesFileReader yesFileReader,
        IBibleRepository bibleRepository,
        ISearchIndex? searchIndex = null)
    {
        _yesFileReader = yesFileReader;
        _bibleRepository = bibleRepository;
        _searchIndex = searchIndex;
    }

    public async Task<Result<BibleVersionDto>> Handle(ImportYesFileCommand request, CancellationToken ct)
    {
        if (request.FileStream is null || !request.FileStream.CanRead)
        {
            return Result<BibleVersionDto>.Failure("Input stream is null or unreadable.");
        }

        try
        {
            var metadata = await _yesFileReader.ReadMetadataAsync(request.FileStream, ct).ConfigureAwait(false);
            var verses = _yesFileReader.ReadVersesAsync(request.FileStream, ct);
            var pericopes = _yesFileReader.ReadPericopesAsync(request.FileStream, ct);

            await _bibleRepository.SaveVersionAsync(metadata, verses, pericopes, ct).ConfigureAwait(false);

            if (_searchIndex is not null)
            {
                // Re-read verses for search index if stream is seekable
                if (request.FileStream.CanSeek)
                {
                    request.FileStream.Seek(0, SeekOrigin.Begin);
                    var indexVerses = _yesFileReader.ReadVersesAsync(request.FileStream, ct);
                    await _searchIndex.IndexVersionAsync(metadata.Id, indexVerses, ct).ConfigureAwait(false);
                }
            }

            var dto = new BibleVersionDto(
                metadata.Id,
                metadata.Title,
                metadata.Language,
                metadata.Description,
                metadata.Books.Count
            );

            return Result<BibleVersionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<BibleVersionDto>.Failure($"Failed to import .yes file: {ex.Message}");
        }
    }
}
