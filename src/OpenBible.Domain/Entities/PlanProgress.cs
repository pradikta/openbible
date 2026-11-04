namespace OpenBible.Domain.Entities;

public sealed record PlanProgress(string PlanId, DateTimeOffset StartedAt, IReadOnlySet<int> CompletedDays);