using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Plans;

public sealed class ToggleReadingPlanDayHandler(IReadingContentSource content, IStudyDataRepository study)
    : IRequestHandler<ToggleReadingPlanDayCommand, Result>
{
    public async Task<Result> Handle(ToggleReadingPlanDayCommand request, CancellationToken ct)
    {
        var plan = content.GetPlans().FirstOrDefault(p => p.Id == request.PlanId);
        if (plan is null || plan.Days.All(d => d.Day != request.Day))
        {
            return Result.Failure("Reading plan day not found.");
        }

        var progress = await study.GetPlanProgressAsync(plan.Id, ct).ConfigureAwait(false);
        var completed = new HashSet<int>(progress?.CompletedDays ?? new HashSet<int>());
        if (!completed.Remove(request.Day))
        {
            completed.Add(request.Day);
        }

        await study.SavePlanProgressAsync(
            new PlanProgress(plan.Id, progress?.StartedAt ?? DateTimeOffset.UtcNow, completed), ct).ConfigureAwait(false);
        return Result.Success();
    }
}
