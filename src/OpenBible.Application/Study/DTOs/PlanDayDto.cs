namespace OpenBible.Application.Study.DTOs;

public sealed record PlanDayDto(int Day, IReadOnlyList<PlanReadingDto> Readings, bool IsCompleted);
