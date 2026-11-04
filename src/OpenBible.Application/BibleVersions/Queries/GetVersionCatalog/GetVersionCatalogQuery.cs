using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Queries.GetVersionCatalog;

/// <summary>Lists downloadable translations, optionally filtered by a search text.</summary>
public sealed record GetVersionCatalogQuery(string? Search = null) : IRequest<Result<IReadOnlyList<CatalogVersionDto>>>;
