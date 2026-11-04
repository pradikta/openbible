using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;
using OpenBible.Domain.Catalog;

namespace OpenBible.Application.Reading.Queries.GetBookList;

public sealed class GetBookListHandler : IRequestHandler<GetBookListQuery, Result<IReadOnlyList<BookListItemDto>>>
{
    public Task<Result<IReadOnlyList<BookListItemDto>>> Handle(GetBookListQuery request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<BookListItemDto> books = BibleBookCatalog.All
            .Select(book => new BookListItemDto(
                book.Id,
                book.ShortName,
                book.FullName,
                book.ChapterCount,
                book.Testament))
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<BookListItemDto>>.Success(books));
    }
}
