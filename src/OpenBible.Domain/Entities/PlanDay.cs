using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Entities;

public sealed record PlanDay(int Day, IReadOnlyList<VerseRange> Readings);