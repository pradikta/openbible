using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Sources;
using OpenBible.Server.Sources;

var builder = WebApplication.CreateBuilder(args);

var sourcesFile = Environment.GetEnvironmentVariable("OPENBIBLE_SOURCES_FILE");
builder.Configuration.AddJsonFile(
    string.IsNullOrWhiteSpace(sourcesFile) ? "sources.json" : sourcesFile,
    optional: true,
    reloadOnChange: false);

builder.Services.AddSingleton<SourceDownloadValidator>();
builder.Services.AddSingleton<ISourceRegistry>(sp => new SourceRegistry(
    builder.Configuration.GetSection("Sources").Get<List<SourceOptions>>() ?? [],
    sp.GetRequiredService<ILogger<SourceRegistry>>()));
builder.Services.AddSingleton<ISourceCatalog, SourceCatalogService>();

builder.Services.AddHttpClient(SourceCatalogService.ClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services
    .AddHttpClient(SourceEndpoints.DownloadClientName, client => client.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });

var app = builder.Build();

app.MapStaticAssets();
app.MapSourceEndpoints();

// Lets the deploy workflow confirm which build is actually serving traffic.
app.MapGet("/version.json", () => Results.Json(new
{
    revision = Environment.GetEnvironmentVariable("OPENBIBLE_REVISION") ?? "dev",
    buildDate = Environment.GetEnvironmentVariable("OPENBIBLE_BUILD_DATE") ?? "unknown",
}));
app.MapFallbackToFile("index.html");

app.Run();
