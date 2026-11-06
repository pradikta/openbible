using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.PreviewYesFile;

public sealed class PreviewYesFileHandler : IRequestHandler<PreviewYesFileQuery, Result<BibleVersionDto>>
{
    private readonly IBibleFormatReaderSelector _formatReaders;

    public PreviewYesFileHandler(IBibleFormatReaderSelector formatReaders)
    {
        _formatReaders = formatReaders;
    }

    public async Task<Result<BibleVersionDto>> Handle(PreviewYesFileQuery request, CancellationToken ct)
    {
        if (request.FileStream is null || !request.FileStream.CanRead)
        {
            return Result<BibleVersionDto>.Failure("Input stream is null or unreadable.");
        }

        var format = _formatReaders.DetectFormat(request.FileStream);
        if (format is null)
        {
            return Result<BibleVersionDto>.Failure(
                "Unsupported translation file. Use .yes, .yes.gz, USFM (.usfm/.sfm or a ZIP), or a SWORD package.");
        }

        try
        {
            var reader = _formatReaders.GetReader(format.Value);
            var metadata = await reader.ReadMetadataAsync(request.FileStream, ct).ConfigureAwait(false);
            return Result<BibleVersionDto>.Success(new BibleVersionDto(
                metadata.Id,
                metadata.Title,
                metadata.Language,
                metadata.Description,
                metadata.Books.Count,
                metadata.Format,
                metadata.SourceId,
                metadata.License));
        }
        catch (NotSupportedException)
        {
            return Result<BibleVersionDto>.Failure("This Bible format is not supported yet.");
        }
        catch (Exception ex)
        {
            return Result<BibleVersionDto>.Failure($"Unable to read translation file: {ex.Message}");
        }
    }
}
