namespace OpenBible.Application.Study.DTOs;

public sealed record PlanDetailDto(string Id, string Title, string Description, IReadOnlyList<PlanDayDto> Days);
