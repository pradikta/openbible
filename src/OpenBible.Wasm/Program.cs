using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenBible.Application;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure;
using OpenBible.Infrastructure.Catalog;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.Persistence.Storage;
using OpenBible.Wasm;
using OpenBible.Wasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Composition Root: Register Application CQRS and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddSingleton<IPersistentStore, IndexedDbPersistentStore>();
builder.Services.AddInfrastructure();
builder.Services.AddSingleton<IVersionDownloader>(
    _ => new HttpVersionDownloader(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }));

builder.Services.AddScoped<BundledBibleSeeder>();

var host = builder.Build();
await host.Services.GetRequiredService<PersistenceInitializer>().RestoreAsync();
await using (var scope = host.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<BundledBibleSeeder>().EnsureSeededAsync();
}
await host.RunAsync();
