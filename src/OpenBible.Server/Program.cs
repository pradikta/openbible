using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Catalog;
using OpenBible.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IVersionCatalog, BundledVersionCatalog>();
builder.Services.AddHttpClient(CatalogDownloadEndpoint.ClientName, client => client.Timeout = TimeSpan.FromMinutes(2));

var app = builder.Build();

app.MapStaticAssets();
app.MapCatalogDownload();
app.MapFallbackToFile("index.html");

app.Run();
