using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Search.DTOs;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Search.Queries.SearchBible;

public sealed class SearchBibleHandler : IRequestHandler<SearchBibleQuery, Result<IReadOnlyList<SearchResultDto>>>
{
    private readonly ISearchIndex? _searchIndex;

    public SearchBibleHandler(ISearchIndex? searchIndex = null)
    {
        _searchIndex = searchIndex;
    }

    public async Task<Result<IReadOnlyList<SearchResultDto>>> Handle(SearchBibleQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.QueryText))
        {
            return Result<IReadOnlyList<SearchResultDto>>.Failure("Query text cannot be empty.");
        }

        if (_searchIndex is null)
        {
            return Result<IReadOnlyList<SearchResultDto>>.Success(Array.Empty<SearchResultDto>());
        }

        var scope = new SearchScope(request.ScopeTestament, request.ScopeBook);
        var matches = await _searchIndex.SearchAsync(request.VersionId, request.QueryText, scope, ct).ConfigureAwait(false);

        var dtos = matches.Select(m => new SearchResultDto(
            m.Reference.ToAri(),
            m.VersionId,
            m.Reference.Book,
            m.Reference.Book.ToString(),
            m.Reference.Chapter,
            m.Reference.Verse,
            m.Text,
            m.Score
        )).ToList();

        return Result<IReadOnlyList<SearchResultDto>>.Success(dtos);
    }
}
