using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.UserData.DTOs;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Application.UserData.Commands.ToggleBookmark;

public sealed class ToggleBookmarkHandler : IRequestHandler<ToggleBookmarkCommand, Result<ToggleBookmarkResultDto>>
{
    private readonly IUserDataRepository _userDataRepository;

    public ToggleBookmarkHandler(IUserDataRepository userDataRepository)
    {
        _userDataRepository = userDataRepository;
    }

    public async Task<Result<ToggleBookmarkResultDto>> Handle(ToggleBookmarkCommand request, CancellationToken ct)
    {
        var reference = new VerseReference(request.BookId, request.Chapter, request.Verse);
        var existing = await _userDataRepository
            .GetBookmarksAsync(request.VersionId, ct)
            .ConfigureAwait(false);

        var match = existing.FirstOrDefault(bookmark => bookmark.Reference == reference);
        if (match is not null)
        {
            await _userDataRepository.DeleteBookmarkAsync(match.Id, ct).ConfigureAwait(false);
            return Result<ToggleBookmarkResultDto>.Success(new ToggleBookmarkResultDto(false, null));
        }

        var createdAt = DateTimeOffset.UtcNow;
        var bookmark = new Bookmark(Guid.NewGuid(), request.VersionId, reference, createdAt);
        await _userDataRepository.SaveBookmarkAsync(bookmark, ct).ConfigureAwait(false);

        var book = BibleBookCatalog.Get(request.BookId);
        var dto = new BookmarkDto(
            bookmark.Id,
            bookmark.VersionId,
            request.BookId,
            book.FullName,
            request.Chapter,
            request.Verse,
            reference.ToAri(),
            createdAt,
            bookmark.Note);

        return Result<ToggleBookmarkResultDto>.Success(new ToggleBookmarkResultDto(true, dto));
    }
}
