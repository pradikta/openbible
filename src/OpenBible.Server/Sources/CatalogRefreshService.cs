using OpenBible.Domain.Interfaces;

namespace OpenBible.Server.Sources;

/// <summary>
/// Refreshes source catalogs on a schedule and warms the local cache at startup.
/// A failed refresh keeps the previous cache so the app can serve stale data.
/// </summary>
public sealed class CatalogRefreshService(
    ISourceRegistry registry,
    SourceCatalogService catalog,
    ILogger<CatalogRefreshService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastRefresh = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var source in registry.Sources.Where(source => source.RefreshMinutes > 0))
            {
                var due = !lastRefresh.TryGetValue(source.Id, out var refreshedAt)
                    || DateTimeOffset.UtcNow - refreshedAt >= TimeSpan.FromMinutes(source.RefreshMinutes);
                if (!due)
                {
                    continue;
                }

                try
                {
                    await catalog.RefreshAsync(source, stoppingToken).ConfigureAwait(false);
                    lastRefresh[source.Id] = DateTimeOffset.UtcNow;
                    logger.LogInformation("Refreshed the catalog for {SourceId}.", source.Id);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not refresh the catalog for {SourceId}.", source.Id);
                }
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
