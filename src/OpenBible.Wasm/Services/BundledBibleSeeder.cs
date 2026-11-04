using MediatR;
using Microsoft.Extensions.Logging;
using OpenBible.Application.BibleVersions.Commands.ImportYesFile;
using OpenBible.Application.BibleVersions.Commands.SetDefaultVersion;
using OpenBible.Application.BibleVersions.Queries.GetDefaultVersion;
using OpenBible.Application.BibleVersions.Queries.GetInstalledVersions;

namespace OpenBible.Wasm.Services;

/// <summary>
/// Imports the King James Version shipped with the app on the first run, so the reader works
/// offline before the user installs anything. Skipped once any version is installed.
/// </summary>
public sealed class BundledBibleSeeder(IMediator mediator, HttpClient http, ILogger<BundledBibleSeeder> logger)
{
    public const string AssetPath = "data/en-kjv.yes.gz";

    public async Task EnsureSeededAsync(CancellationToken ct = default)
    {
        var installed = await mediator.Send(new GetInstalledVersionsQuery(), ct).ConfigureAwait(false);
        if (!installed.IsSuccess || installed.Value is not { Count: 0 })
        {
            return;
        }

        try
        {
            await using var stream = await http.GetStreamAsync(AssetPath, ct).ConfigureAwait(false);
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct).ConfigureAwait(false);
            buffer.Position = 0;

            var imported = await mediator
                .Send(new ImportYesFileCommand(buffer, "en-kjv.yes.gz"), ct)
                .ConfigureAwait(false);
            if (!imported.IsSuccess || imported.Value is null)
            {
                logger.LogWarning("Bundled KJV import failed: {Error}", imported.Error);
                return;
            }

            var current = await mediator.Send(new GetDefaultVersionQuery(), ct).ConfigureAwait(false);
            if (current.IsSuccess && current.Value is null)
            {
                await mediator.Send(new SetDefaultVersionCommand(imported.Value.Id), ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            logger.LogWarning(ex, "Bundled KJV asset could not be loaded.");
        }
    }
}
