using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Plans;

public sealed class GetReadingPlanHandler(IReadingContentSource content, IStudyDataRepository study)
    : IRequestHandler<GetReadingPlanQuery, Result<PlanDetailDto>>
{
    public async Task<Result<PlanDetailDto>> Handle(GetReadingPlanQuery request, CancellationToken ct)
    {
        var plan = content.GetPlans().FirstOrDefault(p => p.Id == request.PlanId);
        if (plan is null)
        {
            return Result<PlanDetailDto>.Failure("Reading plan not found.");
        }

        var progress = await study.GetPlanProgressAsync(plan.Id, ct).ConfigureAwait(false);
        IReadOnlyList<PlanDayDto> days = [.. plan.Days.Select(d => new PlanDayDto(
            d.Day,
            [.. d.Readings.Select(r => new PlanReadingDto(
                r.Start.Book,
                BibleBookCatalog.Get(r.Start.Book).FullName,
                r.Start.Chapter,
                r.Start.Verse,
                r.End.Verse))],
            progress?.CompletedDays.Contains(d.Day) ?? false))];

        return Result<PlanDetailDto>.Success(new PlanDetailDto(plan.Id, plan.Title, plan.Description, days));
    }
}
