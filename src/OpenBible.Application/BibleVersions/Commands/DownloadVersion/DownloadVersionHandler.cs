using MediatR;
using OpenBible.Application.BibleVersions.Commands.ImportYesFile;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Commands.DownloadVersion;

public sealed class DownloadVersionHandler(IVersionCatalog catalog, IVersionDownloader downloader, ISender sender)
    : IRequestHandler<DownloadVersionCommand, Result<BibleVersionDto>>
{
    public async Task<Result<BibleVersionDto>> Handle(DownloadVersionCommand request, CancellationToken ct)
    {
        var entry = catalog.GetAll().FirstOrDefault(v =>
            string.Equals(v.PresetName, request.PresetName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return Result<BibleVersionDto>.Failure("Version is not in the catalog.");
        }

        Stream stream;
        try
        {
            stream = await downloader.DownloadAsync(entry.PresetName, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Result<BibleVersionDto>.Failure(ex.Message);
        }

        await using (stream.ConfigureAwait(false))
        {
            return await sender.Send(new ImportYesFileCommand(stream, entry.PresetName), ct).ConfigureAwait(false);
        }
    }
}
