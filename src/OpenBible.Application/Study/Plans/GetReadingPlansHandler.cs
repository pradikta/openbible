using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Plans;

public sealed class GetReadingPlansHandler(IReadingContentSource content, IStudyDataRepository study)
    : IRequestHandler<GetReadingPlansQuery, Result<IReadOnlyList<PlanSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<PlanSummaryDto>>> Handle(GetReadingPlansQuery request, CancellationToken ct)
    {
        var list = new List<PlanSummaryDto>();
        foreach (var plan in content.GetPlans())
        {
            var progress = await study.GetPlanProgressAsync(plan.Id, ct).ConfigureAwait(false);
            list.Add(new PlanSummaryDto(plan.Id, plan.Title, plan.Description, plan.Days.Count, progress?.CompletedDays.Count ?? 0));
        }

        return Result<IReadOnlyList<PlanSummaryDto>>.Success(list);
    }
}
