using OpenBible.Infrastructure.Catalog;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class VersionCatalogTests
{
    [Fact]
    public void BundledCatalog_LoadsEntriesWithDownloadUrls()
    {
        var items = new BundledVersionCatalog().GetAll();

        Assert.True(items.Count > 100);
        Assert.All(items, i =>
        {
            Assert.False(string.IsNullOrWhiteSpace(i.PresetName));
            Assert.StartsWith("https://", i.DownloadUrl);
            Assert.EndsWith(i.PresetName, i.DownloadUrl);
        });
    }

    [Fact]
    public async Task AppSettings_PersistDefaultVersion()
    {
        var store = new InMemoryPersistentStore();
        await new PersistentAppSettingsRepository(store).SetDefaultVersionIdAsync("kjv");

        var restored = new PersistentAppSettingsRepository(store);
        await restored.RestoreAsync();

        Assert.Equal("kjv", await restored.GetDefaultVersionIdAsync());
    }
}
