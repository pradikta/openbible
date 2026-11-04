using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Application.Reading.Queries.GetChapter;

public sealed class GetChapterHandler : IRequestHandler<GetChapterQuery, Result<ChapterDto>>
{
    private readonly IBibleRepository _bibleRepository;
    private readonly IUserDataRepository? _userDataRepository;

    public GetChapterHandler(IBibleRepository bibleRepository, IUserDataRepository? userDataRepository = null)
    {
        _bibleRepository = bibleRepository;
        _userDataRepository = userDataRepository;
    }

    public async Task<Result<ChapterDto>> Handle(GetChapterQuery request, CancellationToken ct)
    {
        var verses = await _bibleRepository
            .GetChapterVersesAsync(request.VersionId, request.BookId, request.Chapter, ct)
            .ConfigureAwait(false);

        if (verses.Count == 0)
        {
            return Result<ChapterDto>.Failure($"Chapter not found: {request.BookId} {request.Chapter}");
        }

        var pericopes = await _bibleRepository
            .GetChapterPericopesAsync(request.VersionId, request.BookId, request.Chapter, ct)
            .ConfigureAwait(false);

        // Fetch chapter highlights and bookmarks if user data repository is configured
        var chapterRef = new VerseReference(request.BookId, request.Chapter, 0);
        var highlights = _userDataRepository is not null
            ? await _userDataRepository.GetHighlightsForChapterAsync(request.VersionId, chapterRef, ct).ConfigureAwait(false)
            : Array.Empty<OpenBible.Domain.Entities.Highlight>();

        var highlightMap = highlights.ToDictionary(h => h.Reference.Verse, h => h.Color);

        var verseDtos = verses.Select(v => new VerseDto(
            v.Reference.Verse,
            v.Text,
            v.Reference.ToAri(),
            IsBookmarked: false,
            Highlight: highlightMap.TryGetValue(v.Reference.Verse, out var color) ? color : null,
            Footnotes: v.Footnotes.Select(f => f.Content).ToList()
        )).ToList();

        var pericopeDtos = pericopes.Select(p => new PericopeDto(
            p.Reference.Verse,
            p.Title,
            p.ParallelPassages
        )).ToList();

        var chapterDto = new ChapterDto(
            request.BookId,
            request.BookId.ToString(),
            request.Chapter,
            verseDtos,
            pericopeDtos
        );

        return Result<ChapterDto>.Success(chapterDto);
    }
}
