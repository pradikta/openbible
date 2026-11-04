namespace OpenBible.Domain.Interfaces;

/// <summary>Application-wide preferences that are not tied to a verse or version.</summary>
public interface IAppSettingsRepository
{
    Task<string?> GetDefaultVersionIdAsync(CancellationToken ct = default);

    Task SetDefaultVersionIdAsync(string? versionId, CancellationToken ct = default);
}
