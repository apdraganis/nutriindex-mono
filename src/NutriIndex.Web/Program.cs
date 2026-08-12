using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NutriIndex.Web;
using NutriIndex.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var baseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
    ? builder.HostEnvironment.BaseAddress
    : apiBaseUrl;

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(baseAddress, UriKind.Absolute)
});
builder.Services.AddScoped<NutriIndexApiClient>();
builder.Services.AddScoped<BarcodeScannerService>();

var host = builder.Build();
Console.WriteLine($"[NutriIndex] WASM started · build {AppBuildInfo.BuildId}");
await host.RunAsync();
