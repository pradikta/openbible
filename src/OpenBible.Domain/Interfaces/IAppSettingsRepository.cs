namespace OpenBible.Domain.Interfaces;

/// <summary>Application-wide preferences that are not tied to a verse or version.</summary>
public interface IAppSettingsRepository
{
    Task<string?> GetDefaultVersionIdAsync(CancellationToken ct = default);

    Task SetDefaultVersionIdAsync(string? versionId, CancellationToken ct = default);

    /// <summary>URL template containing <c>{url}</c>, used to fetch catalog files through a CORS proxy.</summary>
    Task<string?> GetDownloadProxyUrlAsync(CancellationToken ct = default);

    Task SetDownloadProxyUrlAsync(string? proxyUrlTemplate, CancellationToken ct = default);
}
