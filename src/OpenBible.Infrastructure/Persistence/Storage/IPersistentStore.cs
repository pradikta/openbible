namespace OpenBible.Infrastructure.Persistence.Storage;

/// <summary>
/// Minimal asynchronous key/value store. The browser host backs it with IndexedDB;
/// other hosts and tests use <see cref="InMemoryPersistentStore"/>.
/// </summary>
public interface IPersistentStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    Task SetAsync(string key, string value, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}
