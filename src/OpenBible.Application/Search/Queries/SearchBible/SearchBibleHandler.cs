using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.Formatting;
using OpenBible.Application.Search.DTOs;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Search.Queries.SearchBible;

public sealed class SearchBibleHandler : IRequestHandler<SearchBibleQuery, Result<IReadOnlyList<SearchResultDto>>>
{
    private readonly IBibleRepository _bibleRepository;
    private readonly ISearchIndex? _searchIndex;

    public SearchBibleHandler(IBibleRepository bibleRepository, ISearchIndex? searchIndex = null)
    {
        _bibleRepository = bibleRepository;
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

        var version = await _bibleRepository.GetVersionAsync(request.VersionId, ct).ConfigureAwait(false);

        var dtos = matches.Select(m => new SearchResultDto(
            m.Reference.ToAri(),
            m.VersionId,
            m.Reference.Book,
            ResolveBookName(version, m.Reference.Book),
            m.Reference.Chapter,
            m.Reference.Verse,
            VerseMarkupFormatter.ToPlainText(m.Text),
            m.Score
        )).ToList();

        return Result<IReadOnlyList<SearchResultDto>>.Success(dtos);
    }

    private static string ResolveBookName(BibleVersion? version, BookId bookId)
    {
        var book = version?.Books.FirstOrDefault(b => b.BookId == bookId);
        return book?.FullName ?? OpenBible.Domain.Catalog.BibleBookCatalog.Get(bookId).FullName;
    }
}
