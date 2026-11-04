using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenBible.Application;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Wasm.IndexedDb;
using OpenBible.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Composition Root: Register Application CQRS and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Register IndexedDb Bible Repository (Blazor WASM-specific implementation)
builder.Services.AddSingleton<IBibleRepository, IndexedDbBibleRepository>();

var host = builder.Build();
await host.Services.GetRequiredService<SampleScriptureSeeder>().EnsureSeededAsync();
await host.RunAsync();
