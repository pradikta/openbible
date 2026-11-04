namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredAppSettings(string? DefaultVersionId, string? DownloadProxyUrl = null);
