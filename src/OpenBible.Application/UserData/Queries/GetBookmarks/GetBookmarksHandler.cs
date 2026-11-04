using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.UserData.DTOs;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.UserData.Queries.GetBookmarks;

public sealed class GetBookmarksHandler : IRequestHandler<GetBookmarksQuery, Result<IReadOnlyList<BookmarkDto>>>
{
    private readonly IUserDataRepository _userDataRepository;

    public GetBookmarksHandler(IUserDataRepository userDataRepository)
    {
        _userDataRepository = userDataRepository;
    }

    public async Task<Result<IReadOnlyList<BookmarkDto>>> Handle(GetBookmarksQuery request, CancellationToken ct)
    {
        var bookmarks = await _userDataRepository
            .GetBookmarksAsync(request.VersionId, ct)
            .ConfigureAwait(false);

        IReadOnlyList<BookmarkDto> dtos = bookmarks.Select(bookmark =>
        {
            var book = BibleBookCatalog.Get(bookmark.Reference.Book);
            return new BookmarkDto(
                bookmark.Id,
                bookmark.VersionId,
                bookmark.Reference.Book,
                book.FullName,
                bookmark.Reference.Chapter,
                bookmark.Reference.Verse,
                bookmark.Reference.ToAri(),
                bookmark.CreatedAt,
                bookmark.Note);
        }).ToList();

        return Result<IReadOnlyList<BookmarkDto>>.Success(dtos);
    }
}
