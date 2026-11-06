using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Sources;

namespace OpenBible.Server.Sources;

public sealed class SourceCatalogService : ISourceCatalog
{
    public const string ClientName = "source-catalog";

    private readonly IHttpClientFactory _clients;
    private readonly IWebHostEnvironment _environment;
    private readonly SourceDownloadValidator _validator;

    public SourceCatalogService(
        IHttpClientFactory clients,
        IWebHostEnvironment environment,
        SourceDownloadValidator validator)
    {
        _clients = clients;
        _environment = environment;
        _validator = validator;
    }

    public async Task<IReadOnlyList<SourceTranslation>> GetTranslationsAsync(
        IBibleSource source,
        string? search = null,
        CancellationToken ct = default)
    {
        var translations = await LoadAsync(source, ct).ConfigureAwait(false);
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
        var translations = await LoadAsync(source, ct).ConfigureAwait(false);
        return translations.FirstOrDefault(translation =>
            string.Equals(translation.Id, translationId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<SourceTranslation>> LoadAsync(IBibleSource source, CancellationToken ct)
    {
        var json = source.CatalogKind == SourceCatalogKind.File
            ? await ReadFileAsync(source, ct).ConfigureAwait(false)
            : await ReadLiveAsync(source, ct).ConfigureAwait(false);

        return [.. SourceCatalogParser.Parse(json).Where(translation => !string.IsNullOrWhiteSpace(translation.DownloadUrl))];
    }

    private Task<string> ReadFileAsync(IBibleSource source, CancellationToken ct)
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

        return File.ReadAllTextAsync(path, ct);
    }

    private async Task<string> ReadLiveAsync(IBibleSource source, CancellationToken ct)
    {
        var path = string.IsNullOrWhiteSpace(source.CatalogPath) ? "/" : source.CatalogPath;
        var uri = new Uri(new Uri($"https://{source.Host}"), path);

        if (!await _validator.IsAllowedAsync(uri, source.Host, ct).ConfigureAwait(false))
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

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
