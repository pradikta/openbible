using OpenBible.Domain.Interfaces;

namespace OpenBible.Server;

/// <summary>
/// Streams a catalog translation from the official hosts. The hosts send no CORS headers, so the
/// browser cannot fetch them directly; only presets listed in the bundled catalog are served.
/// </summary>
internal static class CatalogDownloadEndpoint
{
    public const string ClientName = "catalog";

    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "alkitab-host.appspot.com",
        "boafiles.kejut.com"
    };

    public static IEndpointRouteBuilder MapCatalogDownload(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/catalog/download/{presetName}", DownloadAsync);
        return app;
    }

    private static async Task<IResult> DownloadAsync(
        string presetName,
        IVersionCatalog catalog,
        IHttpClientFactory clients,
        HttpContext context,
        CancellationToken ct)
    {
        var entry = catalog.GetAll().FirstOrDefault(v =>
            string.Equals(v.PresetName, presetName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return Results.NotFound();
        }

        using var upstream = await clients.CreateClient(ClientName)
            .GetAsync(entry.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        var finalHost = upstream.RequestMessage?.RequestUri?.Host;
        if (finalHost is null || !AllowedHosts.Contains(finalHost))
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        if (!upstream.IsSuccessStatusCode)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        context.Response.ContentType = "application/octet-stream";
        if (upstream.Content.Headers.ContentLength is { } length)
        {
            context.Response.ContentLength = length;
        }

        await upstream.Content.CopyToAsync(context.Response.Body, ct).ConfigureAwait(false);
        return Results.Empty;
    }
}
