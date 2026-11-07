using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Server.Sources;

public sealed class SourceRegistry : ISourceRegistry
{
    private readonly IReadOnlyList<IBibleSource> _sources;

    public SourceRegistry(IEnumerable<SourceOptions> options, ILogger<SourceRegistry> logger)
    {
        var sources = new List<IBibleSource>();
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option.Id) || string.IsNullOrWhiteSpace(option.Host))
            {
                logger.LogWarning("Skipping source without an id or host.");
                continue;
            }

            if (!Enum.TryParse<BibleFormat>(option.Format, ignoreCase: true, out var format))
            {
                logger.LogWarning("Skipping source {SourceId}: unknown format {Format}.", option.Id, option.Format);
                continue;
            }

            if (!Enum.TryParse<SourceCatalogKind>(option.CatalogKind, ignoreCase: true, out var kind))
            {
                logger.LogWarning("Skipping source {SourceId}: unknown catalog kind {Kind}.", option.Id, option.CatalogKind);
                continue;
            }

            sources.Add(new BibleSource(
                option.Id,
                string.IsNullOrWhiteSpace(option.DisplayName) ? option.Id : option.DisplayName,
                format,
                option.Host,
                kind,
                option.CatalogPath,
                option.CatalogUrl,
                option.CatalogFormat,
                option.DownloadHost,
                option.DownloadUrlFormat,
                Math.Max(0, option.RefreshMinutes),
                Math.Max(1, option.CacheMinutes)));
        }

        _sources = sources;
    }

    public IReadOnlyList<IBibleSource> Sources => _sources;

    public IBibleSource? Find(string sourceId) =>
        _sources.FirstOrDefault(source => string.Equals(source.Id, sourceId, StringComparison.OrdinalIgnoreCase));
}
