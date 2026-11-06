namespace OpenBible.Server.Sources;

public sealed class SourceOptions
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Format { get; set; } = "yes";
    public string Host { get; set; } = string.Empty;
    public string CatalogKind { get; set; } = "live";
    public string? CatalogPath { get; set; }
}
