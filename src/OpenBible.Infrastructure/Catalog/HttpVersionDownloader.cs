using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Catalog;

/// <summary>
/// Downloads catalog files through the user-configured CORS proxy. Catalog hosts send no
/// CORS headers, so a direct browser request is blocked.
/// </summary>
public sealed class HttpVersionDownloader(HttpClient http, IAppSettingsRepository settings) : IVersionDownloader
{
    public const string UrlPlaceholder = "{url}";

    public async Task<Stream> DownloadAsync(string downloadUrl, CancellationToken ct = default)
    {
        var template = await settings.GetDownloadProxyUrlAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new InvalidOperationException("No download proxy is configured.");
        }

        var requestUrl = template.Replace(UrlPlaceholder, Uri.EscapeDataString(downloadUrl), StringComparison.Ordinal);
        try
        {
            using var response = await http.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Download failed ({(int)response.StatusCode}).");
            }

            var buffer = new MemoryStream();
            await response.Content.CopyToAsync(buffer, ct).ConfigureAwait(false);
            buffer.Position = 0;
            return buffer;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Could not reach the download proxy: {ex.Message}", ex);
        }
    }
}
