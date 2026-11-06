using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Queries.GetSourceCatalog;

public sealed record GetSourceCatalogQuery(string SourceId, string? Search = null)
    : IRequest<Result<IReadOnlyList<SourceTranslationDto>>>;
