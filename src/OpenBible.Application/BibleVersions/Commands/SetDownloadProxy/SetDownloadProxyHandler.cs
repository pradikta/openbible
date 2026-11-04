using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Commands.SetDownloadProxy;

public sealed class SetDownloadProxyHandler(IAppSettingsRepository settings)
    : IRequestHandler<SetDownloadProxyCommand, Result>
{
    public async Task<Result> Handle(SetDownloadProxyCommand request, CancellationToken ct)
    {
        await settings.SetDownloadProxyUrlAsync(request.ProxyUrlTemplate, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
