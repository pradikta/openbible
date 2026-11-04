using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.Persistence;

/// <summary>Small built-in plan and devotions so the features work offline before any content is imported.</summary>
public sealed class BundledReadingContent : IReadingContentSource
{
    private static readonly IReadOnlyList<ReadingPlan> Plans =
    [
        new ReadingPlan(
            "gospel-john-21",
            "Gospel of John in 21 days",
            "One chapter of John each day.",
            [.. Enumerable.Range(1, 21).Select(day => new PlanDay(
                day,
                [new VerseRange(new VerseReference(BookId.John, day, 1), new VerseReference(BookId.John, day, 1))]))]),
        new ReadingPlan(
            "psalms-7",
            "Psalms in a week",
            "Seven well-known psalms.",
            [.. new[] { 1, 19, 23, 51, 91, 121, 150 }.Select((psalm, i) => new PlanDay(
                i + 1,
                [new VerseRange(new VerseReference(BookId.Psalms, psalm, 1), new VerseReference(BookId.Psalms, psalm, 1))]))])
    ];

    private static readonly IReadOnlyList<Devotion> Devotions =
    [
        new Devotion("beginning", "In the beginning", "Everything starts with God. Read the first chapter slowly and notice how often it says that what He made was good.", new VerseReference(BookId.Genesis, 1, 1)),
        new Devotion("shepherd", "The Lord is my shepherd", "Rest and provision are gifts, not rewards. Reflect on where you need to be led today.", new VerseReference(BookId.Psalms, 23, 1)),
        new Devotion("word", "The Word became flesh", "God chose to be near. Consider how you can be present to someone this week.", new VerseReference(BookId.John, 1, 1)),
        new Devotion("love", "God so loved the world", "Love is shown in giving. Ask what one thing you can give today.", new VerseReference(BookId.John, 3, 16))
    ];

    public IReadOnlyList<ReadingPlan> GetPlans() => Plans;

    public IReadOnlyList<Devotion> GetDevotions() => Devotions;
}