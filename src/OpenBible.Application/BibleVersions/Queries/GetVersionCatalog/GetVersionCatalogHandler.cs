using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Queries.GetVersionCatalog;

public sealed class GetVersionCatalogHandler(IVersionCatalog catalog)
    : IRequestHandler<GetVersionCatalogQuery, Result<IReadOnlyList<CatalogVersionDto>>>
{
    public Task<Result<IReadOnlyList<CatalogVersionDto>>> Handle(GetVersionCatalogQuery request, CancellationToken ct)
    {
        var search = request.Search?.Trim();
        IEnumerable<CatalogVersion> items = catalog.GetAll();
        if (!string.IsNullOrEmpty(search))
        {
            items = items.Where(v =>
                Contains(v.ShortName, search) ||
                Contains(v.LongName, search) ||
                Contains(v.Locale, search) ||
                Contains(v.Description, search));
        }

        IReadOnlyList<CatalogVersionDto> dtos =
            [.. items.Select(v => new CatalogVersionDto(v.PresetName, v.Locale, v.ShortName, v.LongName, v.Description, v.DownloadUrl))];
        return Task.FromResult(Result<IReadOnlyList<CatalogVersionDto>>.Success(dtos));
    }

    private static bool Contains(string value, string search) =>
        value.Contains(search, StringComparison.OrdinalIgnoreCase);
}
