using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.GetSources;

public sealed class GetSourcesHandler(ISourceDirectory directory)
    : IRequestHandler<GetSourcesQuery, Result<IReadOnlyList<BibleSourceDto>>>
{
    public async Task<Result<IReadOnlyList<BibleSourceDto>>> Handle(GetSourcesQuery request, CancellationToken ct)
    {
        try
        {
            var sources = await directory.GetSourcesAsync(ct).ConfigureAwait(false);
            IReadOnlyList<BibleSourceDto> dtos =
                [.. sources.Select(source => new BibleSourceDto(source.Id, source.DisplayName, source.Format, source.CatalogKind))];
            return Result<IReadOnlyList<BibleSourceDto>>.Success(dtos);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            return Result<IReadOnlyList<BibleSourceDto>>.Failure($"Could not load sources: {ex.Message}");
        }
    }
}
