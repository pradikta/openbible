using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.GetInstalledVersions;

public sealed class GetInstalledVersionsHandler : IRequestHandler<GetInstalledVersionsQuery, Result<IReadOnlyList<BibleVersionDto>>>
{
    private readonly IBibleRepository _bibleRepository;

    public GetInstalledVersionsHandler(IBibleRepository bibleRepository)
    {
        _bibleRepository = bibleRepository;
    }

    public async Task<Result<IReadOnlyList<BibleVersionDto>>> Handle(GetInstalledVersionsQuery request, CancellationToken ct)
    {
        var versions = await _bibleRepository.GetInstalledVersionsAsync(ct).ConfigureAwait(false);

        var dtos = versions.Select(v => new BibleVersionDto(
            v.Id,
            v.Title,
            v.Language,
            v.Description,
            v.Books.Count,
            v.Format,
            v.SourceId,
            v.License
        )).ToList();

        return Result<IReadOnlyList<BibleVersionDto>>.Success(dtos);
    }
}
