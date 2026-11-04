using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.Persistence;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SearchQueryTests
{
    private static readonly string[] Texts =
    [
        "For God so loved the world, that he gave his only begotten Son.",
        "Beloved, let us love one another: for love is of God.",
        "In the beginning God created the heaven and the earth.",
        "God @<f1@>said@/, Let there be light.",
    ];

    private static async Task<SimpleSearchIndex> CreateIndexAsync()
    {
        var index = new SimpleSearchIndex();
        await index.IndexVersionAsync("v", ToAsync(), CancellationToken.None);
        return index;
    }

    private static async IAsyncEnumerable<Verse> ToAsync()
    {
        for (var i = 0; i < Texts.Length; i++)
        {
            yield return new Verse(new VerseReference(BookId.John, 3, i + 1), Texts[i]);
        }

        await Task.CompletedTask;
    }

    private static async Task<int[]> SearchVersesAsync(string query)
    {
        var index = await CreateIndexAsync();
        var matches = await index.SearchAsync("v", query);
        return [.. matches.Select(m => m.Reference.Verse)];
    }

    [Fact]
    public async Task OrdinaryTerms_AreAndedSubstringsIgnoringCase()
    {
        Assert.Equal(new[] { 1, 2 }, await SearchVersesAsync("LOVE god"));
    }

    [Fact]
    public async Task PlusPrefix_MatchesWholeWordOnly()
    {
        Assert.Equal(new[] { 1, 2 }, await SearchVersesAsync("love"));
        Assert.Equal(new[] { 2 }, await SearchVersesAsync("+love"));
    }

    [Fact]
    public async Task QuotedPhrase_MatchesWordSequenceIgnoringPunctuation()
    {
        Assert.Equal(new[] { 1 }, await SearchVersesAsync("\"the world that he\""));
    }

    [Fact]
    public async Task ConsecutivePlusWords_FormPhrase()
    {
        Assert.Equal(new[] { 3 }, await SearchVersesAsync("+the +beginning"));
        Assert.Empty(await SearchVersesAsync("+beginning +the"));
    }

    [Fact]
    public async Task Phrase_IgnoresInlineFormattingMarkers()
    {
        Assert.Equal(new[] { 4 }, await SearchVersesAsync("\"god said let\""));
    }

    [Fact]
    public async Task BlankQuery_ReturnsNothing()
    {
        Assert.Empty(await SearchVersesAsync("   "));
    }
}
