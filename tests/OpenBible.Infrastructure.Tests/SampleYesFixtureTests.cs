using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.YesFile;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SampleYesFixtureTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "sample.yes");

    [Fact]
    public async Task Fixture_ReadsMetadataAndVerses()
    {
        var parser = new YesFileParser();

        await using var metadataStream = File.OpenRead(FixturePath);
        var version = await parser.ReadMetadataAsync(metadataStream);

        await using var verseStream = File.OpenRead(FixturePath);
        var verses = new List<Verse>();
        await foreach (var verse in parser.ReadVersesAsync(verseStream))
        {
            verses.Add(verse);
        }

        Assert.Equal("Fixture Sample", version.Title);
        Assert.Equal(3, verses.Count);
        Assert.All(verses, v => Assert.Equal(BookId.Genesis, v.Reference.Book));
        Assert.Equal("And God said, Let there be light: and there was light.", verses[2].Text);
    }

    [Fact]
    public async Task Fixture_ImportedVersionIsReadableAndSearchable()
    {
        var parser = new YesFileParser();
        var repository = new InMemoryBibleRepository();
        var index = new SimpleSearchIndex();

        await using var metadataStream = File.OpenRead(FixturePath);
        var version = await parser.ReadMetadataAsync(metadataStream);
        await using var verseStream = File.OpenRead(FixturePath);
        await repository.SaveVersionAsync(version, parser.ReadVersesAsync(verseStream));

        var chapter = await repository.GetChapterVersesAsync(version.Id, BookId.Genesis, 1);
        await index.IndexVersionAsync(version.Id, ToAsync(chapter));
        var matches = await index.SearchAsync(version.Id, "\"let there be light\"");

        Assert.Equal(3, chapter.Count);
        Assert.Single(matches);
    }

    private static async IAsyncEnumerable<Verse> ToAsync(IEnumerable<Verse> verses)
    {
        foreach (var verse in verses)
        {
            yield return verse;
        }

        await Task.CompletedTask;
    }
}
