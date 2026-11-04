using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.PreviewYesFile;

public sealed class PreviewYesFileHandler : IRequestHandler<PreviewYesFileQuery, Result<BibleVersionDto>>
{
    private readonly IYesFileReader _yesFileReader;

    public PreviewYesFileHandler(IYesFileReader yesFileReader)
    {
        _yesFileReader = yesFileReader;
    }

    public async Task<Result<BibleVersionDto>> Handle(PreviewYesFileQuery request, CancellationToken ct)
    {
        if (request.FileStream is null || !request.FileStream.CanRead)
        {
            return Result<BibleVersionDto>.Failure("Input stream is null or unreadable.");
        }

        if (!IsYesFile(request.FileName))
        {
            return Result<BibleVersionDto>.Failure("Choose a .yes or .yes.gz translation file.");
        }

        try
        {
            var metadata = await _yesFileReader.ReadMetadataAsync(request.FileStream, ct).ConfigureAwait(false);
            return Result<BibleVersionDto>.Success(new BibleVersionDto(
                metadata.Id,
                metadata.Title,
                metadata.Language,
                metadata.Description,
                metadata.Books.Count));
        }
        catch (Exception ex)
        {
            return Result<BibleVersionDto>.Failure($"Unable to read translation file: {ex.Message}");
        }
    }

    private static bool IsYesFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return fileName.EndsWith(".yes", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".yes.gz", StringComparison.OrdinalIgnoreCase);
    }
}
