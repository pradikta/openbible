using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.GetDefaultVersion;

public sealed class GetDefaultVersionHandler(IAppSettingsRepository settings, IBibleRepository bible)
    : IRequestHandler<GetDefaultVersionQuery, Result<string?>>
{
    public async Task<Result<string?>> Handle(GetDefaultVersionQuery request, CancellationToken ct)
    {
        var id = await settings.GetDefaultVersionIdAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(id))
        {
            return Result<string?>.Success(null);
        }

        var installed = await bible.GetVersionAsync(id, ct).ConfigureAwait(false);
        return Result<string?>.Success(installed is null ? null : id);
    }
}
