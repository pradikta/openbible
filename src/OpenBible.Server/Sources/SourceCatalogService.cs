using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Sources;

namespace OpenBible.Server.Sources;

public sealed class SourceCatalogService : ISourceCatalog
{
    public const string ClientName = "source-catalog";

    private const string DefaultCatalogFormat = "catalog-json";

    private readonly IHttpClientFactory _clients;
    private readonly IWebHostEnvironment _environment;
    private readonly SourceDownloadValidator _validator;
    private readonly ILogger<SourceCatalogService> _logger;
    private readonly string _cacheDirectory;
    private readonly ConcurrentDictionary<string, CachedCatalog> _memory = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public SourceCatalogService(
        IHttpClientFactory clients,
        IWebHostEnvironment environment,
        SourceDownloadValidator validator,
        IConfiguration configuration,
        ILogger<SourceCatalogService> logger)
    {
        _clients = clients;
        _environment = environment;
        _validator = validator;
        _logger = logger;

        var configuredDirectory = configuration["CatalogCache:Directory"];
        _cacheDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(environment.ContentRootPath, "catalog-cache")
            : (Path.IsPathRooted(configuredDirectory)
                ? configuredDirectory
                : Path.Combine(environment.ContentRootPath, configuredDirectory));
    }

    public async Task<IReadOnlyList<SourceTranslation>> GetTranslationsAsync(
        IBibleSource source,
        string? search = null,
        CancellationToken ct = default)
    {
        var translations = await GetItemsAsync(source, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(search))
        {
            return translations;
        }

        var term = search.Trim();
        return [.. translations.Where(translation =>
            Contains(translation.Id, term)
            || Contains(translation.DisplayName, term)
            || Contains(translation.Language, term)
            || Contains(translation.Description, term))];
    }

    public async Task<SourceTranslation?> GetTranslationAsync(
        IBibleSource source,
        string translationId,
        CancellationToken ct = default)
    {
        var translations = await GetItemsAsync(source, ct).ConfigureAwait(false);
        return translations.FirstOrDefault(translation =>
            string.Equals(translation.Id, translationId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task RefreshAsync(IBibleSource source, CancellationToken ct = default)
    {
        await _refreshGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var items = await FetchAsync(source, ct).ConfigureAwait(false);
            Store(source, items);
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task<IReadOnlyList<SourceTranslation>> GetItemsAsync(IBibleSource source, CancellationToken ct)
    {
        if (_memory.TryGetValue(source.Id, out var cached) && !IsExpired(source, cached))
        {
            return cached.Items;
        }

        if (!_memory.ContainsKey(source.Id))
        {
            var disk = await LoadDiskAsync(source, ct).ConfigureAwait(false);
            if (disk is not null)
            {
                _memory[source.Id] = disk;
                if (!IsExpired(source, disk))
                {
                    return disk.Items;
                }
            }
        }

        try
        {
            await RefreshAsync(source, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_memory.TryGetValue(source.Id, out var stale))
            {
                _logger.LogWarning(ex, "Could not refresh source {SourceId}; serving the cached catalog.", source.Id);
                return stale.Items;
            }

            throw;
        }

        return _memory.TryGetValue(source.Id, out var fresh) ? fresh.Items : [];
    }

    private async Task<IReadOnlyList<SourceTranslation>> FetchAsync(IBibleSource source, CancellationToken ct)
    {
        var content = source.CatalogKind == SourceCatalogKind.File
            ? await ReadFileAsync(source, ct).ConfigureAwait(false)
            : await ReadLiveAsync(source, ct).ConfigureAwait(false);

        var parsed = Parse(source, content);
        var downloadHost = string.IsNullOrWhiteSpace(source.DownloadHost) ? source.Host : source.DownloadHost;
        var items = parsed
            .Where(translation => !string.IsNullOrWhiteSpace(translation.DownloadUrl))
            .Where(translation => IsAllowedDownload(translation.DownloadUrl, downloadHost))
            .ToList();

        if (items.Count == 0 && parsed.Count > 0)
        {
            throw new InvalidOperationException(
                $"Source '{source.Id}' returned no items with HTTPS download URLs on '{downloadHost}'.");
        }

        return items;
    }

    private static IReadOnlyList<SourceTranslation> Parse(IBibleSource source, string content)
    {
        var format = (source.CatalogFormat ?? DefaultCatalogFormat).Trim().ToLowerInvariant();
        return format switch
        {
            "alkitab-version-config" => AlkitabVersionConfigParser.Parse(content, source.DownloadUrlFormat ?? string.Empty),
            "ebible-html" => EbibleHtmlParser.Parse(
                content,
                source.DownloadUrlFormat ?? "https://ebible.org/Scriptures/$TRANSLATION_ID_usfm.zip"),
            "crosswire-bibles-html" => CrossWireBiblesHtmlParser.Parse(
                content,
                source.DownloadUrlFormat ?? "https://crosswire.org/ftpmirror/pub/sword/packages/rawzip/$TRANSLATION_ID.zip"),
            _ => SourceCatalogParser.Parse(content)
        };
    }

    private async Task<string> ReadFileAsync(IBibleSource source, CancellationToken ct)
    {
        var path = source.CatalogPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Source '{source.Id}' does not configure a catalog path.");
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(_environment.ContentRootPath, path);
        }

        return await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
    }

    private async Task<string> ReadLiveAsync(IBibleSource source, CancellationToken ct)
    {
        var uri = !string.IsNullOrWhiteSpace(source.CatalogUrl)
            ? new Uri(source.CatalogUrl, UriKind.Absolute)
            : new Uri(new Uri($"https://{source.Host}"), string.IsNullOrWhiteSpace(source.CatalogPath) ? "/" : source.CatalogPath);

        if (!await _validator.IsAllowedAsync(uri, uri.Host, ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Source '{source.Id}' catalog host is not allowed.");
        }

        using var response = await _clients
            .CreateClient(ClientName)
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    private async Task<CachedCatalog?> LoadDiskAsync(IBibleSource source, CancellationToken ct)
    {
        var path = CachePath(source);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            var cached = JsonSerializer.Deserialize<CachedCatalog>(json);
            return cached?.Items is null ? null : cached;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void Store(IBibleSource source, IReadOnlyList<SourceTranslation> items)
    {
        var entry = new CachedCatalog(DateTimeOffset.UtcNow, items);
        _memory[source.Id] = entry;

        try
        {
            var path = CachePath(source);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(entry));
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not persist the catalog cache for {SourceId}.", source.Id);
        }
    }

    private string CachePath(IBibleSource source)
    {
        var safeId = new string(source.Id.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').ToArray());
        if (safeId.Length == 0)
        {
            safeId = "source";
        }

        return Path.Combine(_cacheDirectory, $"{safeId}.json");
    }

    private static bool IsExpired(IBibleSource source, CachedCatalog cached)
    {
        var minutes = source.CacheMinutes > 0 ? source.CacheMinutes : 60;
        return DateTimeOffset.UtcNow - cached.FetchedAt >= TimeSpan.FromMinutes(minutes);
    }

    private static bool IsAllowedDownload(string url, string host) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
