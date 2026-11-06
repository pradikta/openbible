using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class VersionMetadataPersistenceTests
{
    [Fact]
    public async Task SavedVersion_RestoresFormatSourceAndLicense()
    {
        var store = new InMemoryPersistentStore();
        var version = new BibleVersion(
            "tb",
            "Terjemahan Baru",
            "id",
            "Indonesian translation",
            null,
            BibleFormat.Usfm,
            "usfm-example",
            "CC BY");

        var writer = new PersistentBibleRepository(new InMemoryBibleRepository(), store);
        await writer.SaveVersionAsync(version, Verses(), null);

        var reader = new PersistentBibleRepository(new InMemoryBibleRepository(), store);
        await reader.RestoreAsync(new SimpleSearchIndex());

        var restored = await reader.GetVersionAsync("tb");

        Assert.NotNull(restored);
        Assert.Equal(BibleFormat.Usfm, restored.Format);
        Assert.Equal("usfm-example", restored.SourceId);
        Assert.Equal("CC BY", restored.License);
    }

    private static async IAsyncEnumerable<Verse> Verses()
    {
        await Task.CompletedTask;
        yield break;
    }
}
