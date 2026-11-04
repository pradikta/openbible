namespace OpenBible.Domain.Entities;

public sealed record ReadingPlan(string Id, string Title, string Description, IReadOnlyList<PlanDay> Days);