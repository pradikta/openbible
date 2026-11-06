using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class AppSettingsPersistenceTests
{
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
