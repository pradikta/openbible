using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.GetVersionMetadata;

public sealed class GetVersionMetadataHandler(IBibleRepository bibleRepository)
    : IRequestHandler<GetVersionMetadataQuery, Result<BibleVersionDto>>
{
    public async Task<Result<BibleVersionDto>> Handle(GetVersionMetadataQuery request, CancellationToken ct)
    {
        var version = await bibleRepository.GetVersionAsync(request.VersionId, ct).ConfigureAwait(false);

        if (version is null)
        {
            return Result<BibleVersionDto>.Failure($"Version not found: {request.VersionId}");
        }

        return Result<BibleVersionDto>.Success(new BibleVersionDto(
            version.Id,
            version.Title,
            version.Language,
            version.Description,
            version.Books.Count,
            version.Format,
            version.SourceId,
            version.License));
    }
}
