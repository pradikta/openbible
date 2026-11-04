namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredPlanProgress(string PlanId, DateTimeOffset StartedAt, int[] CompletedDays);