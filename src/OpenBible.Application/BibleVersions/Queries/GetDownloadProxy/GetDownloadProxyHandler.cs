using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.GetDownloadProxy;

public sealed class GetDownloadProxyHandler(IAppSettingsRepository settings)
    : IRequestHandler<GetDownloadProxyQuery, Result<string?>>
{
    public async Task<Result<string?>> Handle(GetDownloadProxyQuery request, CancellationToken ct) =>
        Result<string?>.Success(await settings.GetDownloadProxyUrlAsync(ct).ConfigureAwait(false));
}
