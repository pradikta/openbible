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

var host = builder.Build();
await host.Services.GetRequiredService<PersistenceInitializer>().RestoreAsync();
await host.Services.GetRequiredService<SampleScriptureSeeder>().EnsureSeededAsync();
await host.RunAsync();
