using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Search.DTOs;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.Search.Queries.SearchBible;

public sealed record SearchBibleQuery(
    string VersionId,
    string QueryText,
    Testament? ScopeTestament = null,
    BookId? ScopeBook = null
) : IRequest<Result<IReadOnlyList<SearchResultDto>>>;
