using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.Study.Plans;

public sealed record ToggleReadingPlanDayCommand(string PlanId, int Day) : IRequest<Result>;
