using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using NutriIndex.Api.Services;
using NutriIndex.Core;
using NutriIndex.Core.Models;
using NutriIndex.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust reverse proxies (Docker / Fly.io). Without this, every client shares one IP for rate limits.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IOpenFoodFactsClient, OpenFoodFactsClient>(client =>
{
    client.BaseAddress = new Uri("https://world.openfoodfacts.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("NutriIndex/1.0 (MVP; contact: dev@local)");
});
builder.Services.AddSingleton<ProductLookupService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please try again later." },
            cancellationToken);
    };

    options.AddPolicy("products", httpContext =>
    {
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = configuration.GetValue("RateLimiting:Products:PermitLimit", 30);
        var windowSeconds = configuration.GetValue("RateLimiting:Products:WindowSeconds", 60);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
            });
    });

    options.AddPolicy("calculate", httpContext =>
    {
        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = configuration.GetValue("RateLimiting:Calculate:PermitLimit", 60);
        var windowSeconds = configuration.GetValue("RateLimiting:Calculate:WindowSeconds", 60);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
            });
    });
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

app.UseForwardedHeaders();

if (corsOrigins is { Length: > 0 })
    app.UseCors();

app.UseRateLimiter();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet("/api/products/{barcode}", async (string barcode, ProductLookupService products, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(barcode))
        return Results.BadRequest(new { error = "Barcode is required." });

    var product = await products.GetProductAsync(barcode.Trim(), cancellationToken);
    if (product is null)
        return Results.NotFound(new { error = "Product not found." });

    return Results.Ok(product);
}).RequireRateLimiting("products");

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
}).RequireRateLimiting("calculate");

app.MapGet("/health", () => Results.Ok(new { status = "ok", build = BuildInfo.BuildId }));
app.MapGet("/version", () => Results.Ok(new { app = "NutriIndex", build = BuildInfo.BuildId }));
app.MapFallbackToFile("index.html");

Console.WriteLine($"[NutriIndex] API started · build {BuildInfo.BuildId}");

app.Run();

public partial class Program;
