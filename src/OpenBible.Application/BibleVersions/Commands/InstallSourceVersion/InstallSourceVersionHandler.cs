using MediatR;
using OpenBible.Application.BibleVersions.Commands.ImportYesFile;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Commands.InstallSourceVersion;

public sealed class InstallSourceVersionHandler(ISourceDirectory directory, ISender sender)
    : IRequestHandler<InstallSourceVersionCommand, Result<BibleVersionDto>>
{
    public async Task<Result<BibleVersionDto>> Handle(InstallSourceVersionCommand request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Format))
        {
            return Result<BibleVersionDto>.Failure($"Unsupported Bible format: {request.Format}.");
        }

        Stream stream;
        try
        {
            stream = await directory
                .DownloadAsync(request.SourceId, request.TranslationId, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            return Result<BibleVersionDto>.Failure(ex.Message);
        }

        await using (stream.ConfigureAwait(false))
        {
            return await sender
                .Send(new ImportYesFileCommand(stream, request.TranslationId, null, request.Format, request.SourceId, request.License), ct)
                .ConfigureAwait(false);
        }
    }
}
