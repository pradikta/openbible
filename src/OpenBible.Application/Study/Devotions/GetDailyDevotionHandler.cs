using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Devotions;

public sealed class GetDailyDevotionHandler(IReadingContentSource content)
    : IRequestHandler<GetDailyDevotionQuery, Result<DevotionDto>>
{
    public Task<Result<DevotionDto>> Handle(GetDailyDevotionQuery request, CancellationToken ct)
    {
        var devotions = content.GetDevotions();
        if (devotions.Count == 0)
        {
            return Task.FromResult(Result<DevotionDto>.Failure("No devotions available."));
        }

        var d = devotions[(request.Date.DayOfYear - 1) % devotions.Count];
        var dto = new DevotionDto(
            d.Id, d.Title, d.Body,
            d.Passage.Book, BibleBookCatalog.Get(d.Passage.Book).FullName, d.Passage.Chapter, d.Passage.Verse);
        return Task.FromResult(Result<DevotionDto>.Success(dto));
    }
}
