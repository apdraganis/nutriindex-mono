using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.Extensions.Caching.Memory;
using NutriIndex.Api.Services;
using NutriIndex.Core;
using NutriIndex.Core.Models;
using NutriIndex.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<OpenFoodFactsClient>(client =>
{
    client.BaseAddress = new Uri("https://world.openfoodfacts.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("NutriIndex/1.0 (MVP; contact: dev@local)");
});

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
if (corsOrigins is { Length: > 0 })
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

var app = builder.Build();

if (corsOrigins is { Length: > 0 })
    app.UseCors();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet("/api/products/{barcode}", async (string barcode, OpenFoodFactsClient client, IMemoryCache cache) =>
{
    if (string.IsNullOrWhiteSpace(barcode))
        return Results.BadRequest(new { error = "Barcode is required." });

    var normalizedBarcode = barcode.Trim();
    var cacheKey = $"product:{normalizedBarcode}";

    if (cache.TryGetValue(cacheKey, out ProductInfo? cached) && cached is not null)
        return Results.Ok(cached);

    var product = await client.GetProductAsync(normalizedBarcode);
    if (product is null)
        return Results.NotFound(new { error = "Product not found." });

    cache.Set(cacheKey, product, TimeSpan.FromHours(24));
    return Results.Ok(product);
});

app.MapPost("/api/calculate", (CalculateRequest request) =>
{
    try
    {
        var indices = IndexCalculator.Calculate(request);
        return Results.Ok(indices);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", build = BuildInfo.BuildId }));
app.MapGet("/version", () => Results.Ok(new { app = "NutriIndex", build = BuildInfo.BuildId }));
app.MapFallbackToFile("index.html");

Console.WriteLine($"[NutriIndex] API started · build {BuildInfo.BuildId}");

app.Run();
