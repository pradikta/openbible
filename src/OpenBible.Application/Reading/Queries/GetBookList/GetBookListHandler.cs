using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Reading.Queries.GetBookList;

public sealed class GetBookListHandler(IBibleRepository bibleRepository)
    : IRequestHandler<GetBookListQuery, Result<IReadOnlyList<BookListItemDto>>>
{
    public async Task<Result<IReadOnlyList<BookListItemDto>>> Handle(GetBookListQuery request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.VersionId))
        {
            var version = await bibleRepository.GetVersionAsync(request.VersionId, ct).ConfigureAwait(false);

            if (version is { Books.Count: > 0 })
            {
                IReadOnlyList<BookListItemDto> installedBooks = [.. version.Books
                    .Where(book => Enum.IsDefined(book.BookId))
                    .OrderBy(book => (int)book.BookId)
                    .Select(ToListItem)];

                return Result<IReadOnlyList<BookListItemDto>>.Success(installedBooks);
            }
        }

        IReadOnlyList<BookListItemDto> catalog = [.. BibleBookCatalog.All.Select(info => new BookListItemDto(
            info.Id,
            info.ShortName,
            info.FullName,
            info.ChapterCount,
            info.Testament))];

        return Result<IReadOnlyList<BookListItemDto>>.Success(catalog);
    }

    private static BookListItemDto ToListItem(Book book)
    {
        var catalog = BibleBookCatalog.Get(book.BookId);

        return new BookListItemDto(
            book.BookId,
            string.IsNullOrWhiteSpace(book.ShortName) ? catalog.ShortName : book.ShortName,
            string.IsNullOrWhiteSpace(book.FullName) ? catalog.FullName : book.FullName,
            book.ChapterCount > 0 ? book.ChapterCount : catalog.ChapterCount,
            catalog.Testament);
    }
}
