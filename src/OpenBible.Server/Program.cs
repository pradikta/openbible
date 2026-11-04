using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Catalog;
using OpenBible.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVersionCatalog, BundledVersionCatalog>();
builder.Services.AddHttpClient(CatalogDownloadEndpoint.ClientName, client => client.Timeout = TimeSpan.FromMinutes(2));

var app = builder.Build();

app.MapStaticAssets();
app.MapCatalogDownload();

// Lets the deploy workflow confirm which build is actually serving traffic.
app.MapGet("/version.json", () => Results.Json(new
{
    revision = Environment.GetEnvironmentVariable("OPENBIBLE_REVISION") ?? "dev",
    buildDate = Environment.GetEnvironmentVariable("OPENBIBLE_BUILD_DATE") ?? "unknown",
}));
app.MapFallbackToFile("index.html");

app.Run();
