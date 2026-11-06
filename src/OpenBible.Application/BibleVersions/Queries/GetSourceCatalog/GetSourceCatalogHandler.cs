using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.GetSourceCatalog;

public sealed class GetSourceCatalogHandler(ISourceDirectory directory)
    : IRequestHandler<GetSourceCatalogQuery, Result<IReadOnlyList<SourceTranslationDto>>>
{
    public async Task<Result<IReadOnlyList<SourceTranslationDto>>> Handle(GetSourceCatalogQuery request, CancellationToken ct)
    {
        try
        {
            var translations = await directory
                .GetTranslationsAsync(request.SourceId, request.Search, ct)
                .ConfigureAwait(false);

            IReadOnlyList<SourceTranslationDto> dtos =
                [.. translations.Select(translation => new SourceTranslationDto(
                    request.SourceId,
                    translation.Id,
                    translation.DisplayName,
                    translation.Language,
                    translation.Description,
                    translation.License))];

            return Result<IReadOnlyList<SourceTranslationDto>>.Success(dtos);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            return Result<IReadOnlyList<SourceTranslationDto>>.Failure($"Could not load the source catalog: {ex.Message}");
        }
    }
}
