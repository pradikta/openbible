using System.Net;
using System.Text.Json;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Sources;

/// <summary>Reads the registry and proxies downloads through <c>OpenBible.Server</c>.</summary>
public sealed class HttpSourceDirectory(HttpClient http) : ISourceDirectory
{
    public async Task<IReadOnlyList<IBibleSource>> GetSourcesAsync(CancellationToken ct = default)
    {
        var json = await GetStringAsync("api/sources", ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var sources = new List<IBibleSource>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var id = ReadString(item, "id");
            if (id.Length == 0
                || !Enum.TryParse<BibleFormat>(ReadString(item, "format"), ignoreCase: true, out var format)
                || !Enum.TryParse<SourceCatalogKind>(ReadString(item, "catalogKind"), ignoreCase: true, out var kind))
            {
                continue;
            }

            var name = ReadString(item, "displayName");
            sources.Add(new BibleSource(id, name.Length > 0 ? name : id, format, string.Empty, kind));
        }

        return sources;
    }

    public async Task<IReadOnlyList<SourceTranslation>> GetTranslationsAsync(
        string sourceId,
        string? search = null,
        CancellationToken ct = default)
    {
        var path = $"api/sources/{Uri.EscapeDataString(sourceId)}/catalog";
        if (!string.IsNullOrWhiteSpace(search))
        {
            path += $"?search={Uri.EscapeDataString(search.Trim())}";
        }

        var json = await GetStringAsync(path, ct).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var translations = new List<SourceTranslation>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var id = ReadString(item, "id");
            if (id.Length == 0)
            {
                continue;
            }

            var name = ReadString(item, "displayName");
            translations.Add(new SourceTranslation(
                id,
                name.Length > 0 ? name : id,
                ReadString(item, "language"),
                ReadString(item, "description"),
                ReadNullableString(item, "license"),
                ReadString(item, "downloadUrl")));
        }

        return translations;
    }

    public async Task<Stream> DownloadAsync(string sourceId, string translationId, CancellationToken ct = default)
    {
        try
        {
            using var response = await http
                .GetAsync(
                    $"api/sources/{Uri.EscapeDataString(sourceId)}/download/{Uri.EscapeDataString(translationId)}",
                    HttpCompletionOption.ResponseHeadersRead,
                    ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(response.StatusCode == HttpStatusCode.NotFound
                    ? "The download service is not available on this host. Use manual import instead."
                    : $"Download failed ({(int)response.StatusCode}).");
            }

            var buffer = new MemoryStream();
            await response.Content.CopyToAsync(buffer, ct).ConfigureAwait(false);
            buffer.Position = 0;
            return buffer;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Could not reach the server: {ex.Message}", ex);
        }
    }

    private async Task<string> GetStringAsync(string path, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(path, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Could not reach the server: {ex.Message}", ex);
        }
    }

    private static string ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string? ReadNullableString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
