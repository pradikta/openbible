namespace OpenBible.Server.Sources;

public sealed class SourceOptions
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Format { get; set; } = "yes";
    public string Host { get; set; } = string.Empty;
    public string CatalogKind { get; set; } = "live";
    public string? CatalogPath { get; set; }
    public string? CatalogUrl { get; set; }
    public string? CatalogFormat { get; set; }
    public string? DownloadHost { get; set; }
    public string? DownloadUrlFormat { get; set; }
    public int RefreshMinutes { get; set; }
    public int CacheMinutes { get; set; } = 60;
}
