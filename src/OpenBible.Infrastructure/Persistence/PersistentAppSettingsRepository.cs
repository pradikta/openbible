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
    private string? _downloadProxyUrl;

    public Task<string?> GetDefaultVersionIdAsync(CancellationToken ct = default) => Task.FromResult(_defaultVersionId);

    public Task<string?> GetDownloadProxyUrlAsync(CancellationToken ct = default) => Task.FromResult(_downloadProxyUrl);

    public Task SetDefaultVersionIdAsync(string? versionId, CancellationToken ct = default)
    {
        _defaultVersionId = versionId;
        return SaveAsync(ct);
    }

    public Task SetDownloadProxyUrlAsync(string? proxyUrlTemplate, CancellationToken ct = default)
    {
        _downloadProxyUrl = string.IsNullOrWhiteSpace(proxyUrlTemplate) ? null : proxyUrlTemplate.Trim();
        return SaveAsync(ct);
    }

    private Task SaveAsync(CancellationToken ct) =>
        store.SetAsync(
            Key,
            JsonSerializer.Serialize(
                new StoredAppSettings(_defaultVersionId, _downloadProxyUrl),
                PersistenceJsonContext.Default.StoredAppSettings),
            ct);

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
            var stored = JsonSerializer.Deserialize(json, PersistenceJsonContext.Default.StoredAppSettings);
            _defaultVersionId = stored?.DefaultVersionId;
            _downloadProxyUrl = stored?.DownloadProxyUrl;
        }
        catch (JsonException)
        {
        }
    }
}
