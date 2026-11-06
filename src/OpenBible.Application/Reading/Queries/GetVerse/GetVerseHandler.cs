using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;
using OpenBible.Application.Reading.Queries.GetChapter;

namespace OpenBible.Application.Reading.Queries.GetVerse;

public sealed class GetVerseHandler(IMediator mediator) : IRequestHandler<GetVerseQuery, Result<VerseDto>>
{
    public async Task<Result<VerseDto>> Handle(GetVerseQuery request, CancellationToken ct)
    {
        var chapter = await mediator
            .Send(new GetChapterQuery(request.VersionId, request.BookId, request.Chapter), ct)
            .ConfigureAwait(false);

        if (chapter.IsFailure || chapter.Value is null)
        {
            return Result<VerseDto>.Failure(chapter.Error ?? "Chapter not found.");
        }

        var verse = chapter.Value.Verses.FirstOrDefault(v => v.VerseNumber == request.Verse);

        return verse is not null
            ? Result<VerseDto>.Success(verse)
            : Result<VerseDto>.Failure($"Verse not found: {request.BookId} {request.Chapter}:{request.Verse}");
    }
}
