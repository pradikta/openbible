using System.Text.Json;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Persistence.Serialization;
using OpenBible.Infrastructure.Persistence.Storage;

namespace OpenBible.Infrastructure.Persistence;

/// <summary>Keeps app settings in memory and writes them to the key <c>openbible.settings</c>.</summary>
public sealed class PersistentAppSettingsRepository(IPersistentStore store) : IAppSettingsRepository
{
    private const string Key = "openbible.settings";
    private string? _defaultVersionId;

    public Task<string?> GetDefaultVersionIdAsync(CancellationToken ct = default) => Task.FromResult(_defaultVersionId);

    public async Task SetDefaultVersionIdAsync(string? versionId, CancellationToken ct = default)
    {
        _defaultVersionId = versionId;
        await store.SetAsync(
                Key,
                JsonSerializer.Serialize(new StoredAppSettings(versionId), PersistenceJsonContext.Default.StoredAppSettings),
                ct)
            .ConfigureAwait(false);
    }

    /// <summary>Loads persisted settings; a corrupt value is ignored.</summary>
    public async Task RestoreAsync(CancellationToken ct = default)
    {
        var json = await store.GetAsync(Key, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            _defaultVersionId = JsonSerializer.Deserialize(json, PersistenceJsonContext.Default.StoredAppSettings)?.DefaultVersionId;
        }
        catch (JsonException)
        {
        }
    }
}
