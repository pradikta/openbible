using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Sources;
using OpenBible.Server;
using OpenBible.Server.Sources;

var builder = WebApplication.CreateBuilder(args);

// Adds OPENBIBLE_SOURCES__0__ID, OPENBIBLE_CATALOGCACHE__DIRECTORY, etc.
builder.Configuration.AddEnvironmentVariables(prefix: "OPENBIBLE_");

var sourcesFile = Environment.GetEnvironmentVariable("OPENBIBLE_SOURCES_FILE");
builder.Configuration.AddJsonFile(
    string.IsNullOrWhiteSpace(sourcesFile) ? "sources.json" : sourcesFile,
    optional: true,
    reloadOnChange: false);

var cacheDirectory = Environment.GetEnvironmentVariable("OPENBIBLE_CATALOG_CACHE");
if (!string.IsNullOrWhiteSpace(cacheDirectory))
{
    builder.Configuration["CatalogCache:Directory"] = cacheDirectory;
}

builder.Services.AddSingleton<SourceDownloadValidator>();
builder.Services.AddSingleton<ISourceRegistry>(sp => new SourceRegistry(
    builder.Configuration.GetSection("Sources").Get<List<SourceOptions>>() ?? [],
    sp.GetRequiredService<ILogger<SourceRegistry>>()));
builder.Services.AddSingleton<SourceCatalogService>();
builder.Services.AddSingleton<ISourceCatalog>(sp => sp.GetRequiredService<SourceCatalogService>());
builder.Services.AddHostedService<CatalogRefreshService>();

builder.Services
    .AddHttpClient(SourceCatalogService.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });
builder.Services
    .AddHttpClient(SourceEndpoints.DownloadClientName, client => client.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });

var app = builder.Build();

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENBIBLE_ADMIN_TOKEN"))
    && string.IsNullOrWhiteSpace(builder.Configuration["Admin:Token"]))
{
    app.Logger.LogWarning("OPENBIBLE_ADMIN_TOKEN is not set; the /admin page and its API are unprotected.");
}

app.MapStaticAssets();
app.MapSourceEndpoints();
app.MapAdminEndpoints();

// Lets the deploy workflow confirm which build is actually serving traffic.
app.MapGet("/version.json", () => Results.Json(new
{
    revision = Environment.GetEnvironmentVariable("OPENBIBLE_REVISION") ?? "dev",
    buildDate = Environment.GetEnvironmentVariable("OPENBIBLE_BUILD_DATE") ?? "unknown",
}));
app.MapFallbackToFile("index.html");

app.Run();
