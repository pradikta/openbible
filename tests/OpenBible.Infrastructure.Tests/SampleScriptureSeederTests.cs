using OpenBible.Domain.Enums;
using OpenBible.Infrastructure.Persistence;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class SampleScriptureSeederTests
{
    [Fact]
    public async Task EnsureSeededAsync_LoadsReadableExcerptsAndSearch()
    {
        var repository = new InMemoryBibleRepository();
        var index = new SimpleSearchIndex();
        var seeder = new SampleScriptureSeeder(repository, index);

        await seeder.EnsureSeededAsync();
        await seeder.EnsureSeededAsync();

        var versions = await repository.GetInstalledVersionsAsync();
        var genesis = await repository.GetChapterVersesAsync(SampleScriptureSeeder.VersionId, BookId.Genesis, 1);
        var pericopes = await repository.GetChapterPericopesAsync(SampleScriptureSeeder.VersionId, BookId.John, 1);
        var matches = await index.SearchAsync(SampleScriptureSeeder.VersionId, "shepherd");

        Assert.Single(versions);
        Assert.Equal(66, versions[0].Books.Count);
        Assert.Equal(5, genesis.Count);
        Assert.Contains(pericopes, pericope => pericope.Title == "The Word Became Flesh");
        Assert.Single(matches);
        Assert.Equal(23, matches[0].Reference.Chapter);
    }
}
