using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OpenBible.Application;
using OpenBible.Infrastructure;
using OpenBible.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Composition Root: Register Application CQRS and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

await builder.Build().RunAsync();
