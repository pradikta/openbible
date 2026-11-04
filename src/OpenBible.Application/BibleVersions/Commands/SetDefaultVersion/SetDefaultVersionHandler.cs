using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Commands.SetDefaultVersion;

public sealed class SetDefaultVersionHandler(IAppSettingsRepository settings, IBibleRepository bible)
    : IRequestHandler<SetDefaultVersionCommand, Result>
{
    public async Task<Result> Handle(SetDefaultVersionCommand request, CancellationToken ct)
    {
        var version = await bible.GetVersionAsync(request.VersionId, ct).ConfigureAwait(false);
        if (version is null)
        {
            return Result.Failure("Version is not installed.");
        }

        await settings.SetDefaultVersionIdAsync(version.Id, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
