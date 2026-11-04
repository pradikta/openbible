using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Application.UserData.Commands.SetVerseHighlight;

public sealed class SetVerseHighlightHandler : IRequestHandler<SetVerseHighlightCommand, Result<bool>>
{
    private readonly IUserDataRepository _userDataRepository;

    public SetVerseHighlightHandler(IUserDataRepository userDataRepository)
    {
        _userDataRepository = userDataRepository;
    }

    public async Task<Result<bool>> Handle(SetVerseHighlightCommand request, CancellationToken ct)
    {
        var chapterReference = new VerseReference(request.BookId, request.Chapter, 0);
        var highlights = await _userDataRepository
            .GetHighlightsForChapterAsync(request.VersionId, chapterReference, ct)
            .ConfigureAwait(false);

        var match = highlights.FirstOrDefault(highlight => highlight.Reference.Verse == request.Verse);
        if (match is not null && match.Color == request.Color)
        {
            await _userDataRepository.DeleteHighlightAsync(match.Id, ct).ConfigureAwait(false);
            return Result<bool>.Success(false);
        }

        var reference = new VerseReference(request.BookId, request.Chapter, request.Verse);
        var highlight = new Highlight(
            Guid.NewGuid(),
            request.VersionId,
            reference,
            request.Color,
            DateTimeOffset.UtcNow);

        await _userDataRepository.SaveHighlightAsync(highlight, ct).ConfigureAwait(false);
        return Result<bool>.Success(true);
    }
}
