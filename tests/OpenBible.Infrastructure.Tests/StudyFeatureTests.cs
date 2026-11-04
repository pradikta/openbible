using OpenBible.Application.Study.Compare;
using OpenBible.Application.Study.Devotions;
using OpenBible.Application.Study.Labels;
using OpenBible.Application.Study.Notes;
using OpenBible.Application.Study.Plans;
using OpenBible.Application.Study.ProgressMarks;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class StudyFeatureTests
{
    private static readonly BundledReadingContent Content = new();

    [Fact]
    public async Task Notes_SaveAndDeleteByBlankText()
    {
        var repo = new InMemoryStudyDataRepository();
        var save = new SaveNoteHandler(repo);
        await save.Handle(new SaveNoteCommand("v", BookId.John, 3, 16, "hello"), default);
        var notes = await new GetNotesHandler(repo).Handle(new GetNotesQuery("v"), default);
        Assert.Single(notes.Value!);

        await save.Handle(new SaveNoteCommand("v", BookId.John, 3, 16, " "), default);
        notes = await new GetNotesHandler(repo).Handle(new GetNotesQuery("v"), default);
        Assert.Empty(notes.Value!);
    }

    [Fact]
    public async Task Labels_CreateIsCaseInsensitiveAndMarkerLabelsFilterUnknown()
    {
        var repo = new InMemoryStudyDataRepository();
        var create = new CreateLabelHandler(repo);
        var a = await create.Handle(new CreateLabelCommand("Faith"), default);
        var b = await create.Handle(new CreateLabelCommand("faith"), default);
        Assert.Equal(a.Value!.Id, b.Value!.Id);

        var marker = Guid.NewGuid();
        await new SetMarkerLabelsHandler(repo).Handle(new SetMarkerLabelsCommand(marker, [a.Value.Id, Guid.NewGuid()]), default);
        var ids = await new GetMarkerLabelsHandler(repo).Handle(new GetMarkerLabelsQuery(marker), default);
        Assert.Equal(new[] { a.Value.Id }, ids.Value!);
    }

    [Fact]
    public async Task ProgressMarks_SetAndClear()
    {
        var repo = new InMemoryStudyDataRepository();
        await new SetProgressMarkHandler(repo).Handle(new SetProgressMarkCommand(2, "v", BookId.Psalms, 23, 1), default);
        var marks = await new GetProgressMarksHandler(repo).Handle(new GetProgressMarksQuery(), default);
        Assert.Equal(2, marks.Value!.Single().Slot);

        await new ClearProgressMarkHandler(repo).Handle(new ClearProgressMarkCommand(2), default);
        marks = await new GetProgressMarksHandler(repo).Handle(new GetProgressMarksQuery(), default);
        Assert.Empty(marks.Value!);
    }

    [Fact]
    public async Task Plans_ToggleDayUpdatesProgress()
    {
        var repo = new InMemoryStudyDataRepository();
        var planId = Content.GetPlans()[0].Id;
        var toggle = new ToggleReadingPlanDayHandler(Content, repo);

        Assert.True((await toggle.Handle(new ToggleReadingPlanDayCommand(planId, 1), default)).IsSuccess);
        var list = await new GetReadingPlansHandler(Content, repo).Handle(new GetReadingPlansQuery(), default);
        Assert.Equal(1, list.Value!.First(p => p.Id == planId).CompletedDays);

        await toggle.Handle(new ToggleReadingPlanDayCommand(planId, 1), default);
        var detail = await new GetReadingPlanHandler(Content, repo).Handle(new GetReadingPlanQuery(planId), default);
        Assert.False(detail.Value!.Days[0].IsCompleted);
        Assert.False((await toggle.Handle(new ToggleReadingPlanDayCommand(planId, 9999), default)).IsSuccess);
    }

    [Fact]
    public async Task Devotion_PicksEntryByDayOfYear()
    {
        var handler = new GetDailyDevotionHandler(Content);
        var result = await handler.Handle(new GetDailyDevotionQuery(new DateOnly(2025, 1, 1)), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(Content.GetDevotions()[0].Id, result.Value!.Id);
    }

    [Fact]
    public async Task Compare_ReturnsTextPerInstalledVersion()
    {
        var bible = new InMemoryBibleRepository();
        var reference = new VerseReference(BookId.Genesis, 1, 1);
        await bible.SaveVersionAsync(new BibleVersion("a", "A", "en", ""), One(new Verse(reference, "In the beginning")));
        await bible.SaveVersionAsync(new BibleVersion("b", "B", "en", ""), One(new Verse(reference, "Pada mulanya")));

        var result = await new CompareVerseHandler(bible).Handle(new CompareVerseQuery(BookId.Genesis, 1, 1), default);

        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task StudyData_SurvivesRestart()
    {
        var store = new InMemoryPersistentStore();
        var first = new PersistentStudyDataRepository(new InMemoryStudyDataRepository(), store);
        await first.SaveLabelAsync(new Label(Guid.NewGuid(), "Hope"));
        await first.SaveProgressMarkAsync(new ProgressMark(1, "v", new VerseReference(BookId.John, 1, 1), DateTimeOffset.UtcNow));

        var second = new PersistentStudyDataRepository(new InMemoryStudyDataRepository(), store);
        await second.RestoreAsync();

        Assert.Single(await second.GetLabelsAsync());
        Assert.Single(await second.GetProgressMarksAsync());
    }

    private static async IAsyncEnumerable<Verse> One(Verse verse)
    {
        yield return verse;
        await Task.CompletedTask;
    }
}
