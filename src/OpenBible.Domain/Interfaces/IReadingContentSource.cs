using OpenBible.Domain.Entities;

namespace OpenBible.Domain.Interfaces;

/// <summary>Bundled reading plans and devotions available without a network.</summary>
public interface IReadingContentSource
{
    IReadOnlyList<ReadingPlan> GetPlans();
    IReadOnlyList<Devotion> GetDevotions();
}