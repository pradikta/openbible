using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Catalog;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
using OpenBible.Infrastructure.YesFile;

namespace OpenBible.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the parser, in-memory working stores and the persistence decorators.
    /// Register an <see cref="IPersistentStore"/> after this call (for example IndexedDB in the
    /// browser host) to replace the default in-memory store, then call
    /// <see cref="PersistenceInitializer.RestoreAsync"/> at startup.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IYesFileReader, YesFileParser>();
        services.TryAddSingleton<IPersistentStore, InMemoryPersistentStore>();

        services.AddSingleton<InMemoryBibleRepository>();
        services.AddSingleton<PersistentBibleRepository>();
        services.AddSingleton<IBibleRepository>(sp => sp.GetRequiredService<PersistentBibleRepository>());

        services.AddSingleton<InMemoryUserDataRepository>();
        services.AddSingleton<PersistentUserDataRepository>();
        services.AddSingleton<IUserDataRepository>(sp => sp.GetRequiredService<PersistentUserDataRepository>());

        services.AddSingleton<InMemoryStudyDataRepository>();
        services.AddSingleton<PersistentStudyDataRepository>();
        services.AddSingleton<IStudyDataRepository>(sp => sp.GetRequiredService<PersistentStudyDataRepository>());
        services.AddSingleton<IReadingContentSource, BundledReadingContent>();

        services.AddSingleton<PersistentAppSettingsRepository>();
        services.AddSingleton<IAppSettingsRepository>(sp => sp.GetRequiredService<PersistentAppSettingsRepository>());
        services.AddSingleton<IVersionCatalog, BundledVersionCatalog>();

        services.AddSingleton<ISearchIndex, SimpleSearchIndex>();
        services.AddSingleton<IBackupService, UserDataBackupService>();
        services.AddSingleton<PersistenceInitializer>();

        return services;
    }
}
