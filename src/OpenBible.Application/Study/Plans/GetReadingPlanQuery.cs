using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;

namespace OpenBible.Application.Study.Plans;

public sealed record GetReadingPlanQuery(string PlanId) : IRequest<Result<PlanDetailDto>>;
