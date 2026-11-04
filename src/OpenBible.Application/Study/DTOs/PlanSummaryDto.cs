namespace OpenBible.Application.Study.DTOs;

public sealed record PlanSummaryDto(string Id, string Title, string Description, int TotalDays, int CompletedDays);
