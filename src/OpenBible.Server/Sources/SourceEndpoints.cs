using System.Text.Json;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Sources;

namespace OpenBible.Server.Sources;

internal static class SourceEndpoints
{
    public const string DownloadClientName = "source-download";

    private const long MaxDownloadBytes = 128L * 1024L * 1024L;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/octet-stream",
        "application/gzip",
        "application/x-gzip",
        "application/zip",
        "application/x-zip-compressed",
        "text/plain"
    };

    public static IEndpointRouteBuilder MapSourceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sources", (ISourceRegistry registry) => Results.Json(
            registry.Sources.Select(source => new
            {
                id = source.Id,
                displayName = source.DisplayName,
                format = source.Format.ToString().ToLowerInvariant(),
                catalogKind = source.CatalogKind.ToString().ToLowerInvariant()
            })));

        app.MapGet("/api/sources/{sourceId}/catalog", GetCatalogAsync);
        app.MapGet("/api/sources/{sourceId}/download/{translationId}", DownloadAsync);
        return app;
    }

    private static async Task<IResult> GetCatalogAsync(
        string sourceId,
        string? search,
        ISourceRegistry registry,
        ISourceCatalog catalog,
        CancellationToken ct)
    {
        var source = registry.Find(sourceId);
        if (source is null)
        {
            return Results.NotFound();
        }

        try
        {
            var translations = await catalog.GetTranslationsAsync(source, search, ct).ConfigureAwait(false);
            return Results.Json(translations.Select(translation => new
            {
                id = translation.Id,
                displayName = translation.DisplayName,
                language = translation.Language,
                description = translation.Description,
                license = translation.License,
                downloadUrl = $"/api/sources/{Uri.EscapeDataString(source.Id)}/download/{Uri.EscapeDataString(translation.Id)}"
            }));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or JsonException)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> DownloadAsync(
        string sourceId,
        string translationId,
        ISourceRegistry registry,
        ISourceCatalog catalog,
        SourceDownloadValidator validator,
        IHttpClientFactory clients,
        HttpContext context,
        CancellationToken ct)
    {
        var source = registry.Find(sourceId);
        if (source is null)
        {
            return Results.NotFound();
        }

        SourceTranslation? translation;
        try
        {
            translation = await catalog.GetTranslationAsync(source, translationId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or JsonException)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        if (translation is null)
        {
            return Results.NotFound();
        }

        Uri uri;
        try
        {
            uri = new Uri(translation.DownloadUrl, UriKind.Absolute);
        }
        catch (UriFormatException)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        if (!await validator.IsAllowedAsync(uri, source.Host, ct).ConfigureAwait(false))
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));

        using var upstream = await clients
            .CreateClient(DownloadClientName)
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);

        if (!upstream.IsSuccessStatusCode)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        if (upstream.Content.Headers.ContentLength is { } declaredLength && declaredLength > MaxDownloadBytes)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var mediaType = upstream.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null && !AllowedContentTypes.Contains(mediaType))
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        context.Response.ContentType = mediaType ?? "application/octet-stream";
        if (upstream.Content.Headers.ContentLength is { } contentLength)
        {
            context.Response.ContentLength = contentLength;
        }

        await using var body = await upstream.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await body.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > MaxDownloadBytes)
            {
                throw new InvalidOperationException("Download exceeded the maximum allowed size.");
            }

            await context.Response.Body.WriteAsync(buffer.AsMemory(0, read), timeout.Token).ConfigureAwait(false);
        }

        return Results.Empty;
    }
}
