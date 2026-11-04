using System.Net;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Catalog;

/// <summary>Downloads catalog files through the OpenBible server, which proxies the official catalog hosts.</summary>
public sealed class HttpVersionDownloader(HttpClient http) : IVersionDownloader
{
    public const string DownloadPath = "api/catalog/download/";

    public async Task<Stream> DownloadAsync(string presetName, CancellationToken ct = default)
    {
        try
        {
            using var response = await http
                .GetAsync(DownloadPath + Uri.EscapeDataString(presetName), HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(response.StatusCode == HttpStatusCode.NotFound
                    ? "The download service is not available on this host. Use Manual download and Import."
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
}
