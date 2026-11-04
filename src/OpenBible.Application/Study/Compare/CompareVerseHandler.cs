using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.Formatting;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Compare;

public sealed class CompareVerseHandler(IBibleRepository bible)
    : IRequestHandler<CompareVerseQuery, Result<IReadOnlyList<VerseComparisonDto>>>
{
    public async Task<Result<IReadOnlyList<VerseComparisonDto>>> Handle(CompareVerseQuery request, CancellationToken ct)
    {
        var list = new List<VerseComparisonDto>();
        foreach (var version in await bible.GetInstalledVersionsAsync(ct).ConfigureAwait(false))
        {
            var verses = await bible.GetChapterVersesAsync(version.Id, request.BookId, request.Chapter, ct).ConfigureAwait(false);
            var verse = verses.FirstOrDefault(v => v.Reference.Verse == request.Verse);
            if (verse is not null)
            {
                list.Add(new VerseComparisonDto(version.Id, version.Title, VerseMarkupFormatter.ToPlainText(verse.Text)));
            }
        }

        return Result<IReadOnlyList<VerseComparisonDto>>.Success(list);
    }
}
