using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NutriIndex.Api.Services;

namespace NutriIndex.Api.Tests;

public sealed class NutriIndexApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Host settings are visible to WebApplication.CreateBuilder configuration.
        builder.UseSetting("RateLimiting:Products:PermitLimit", "2");
        builder.UseSetting("RateLimiting:Products:WindowSeconds", "60");
        builder.UseSetting("RateLimiting:Calculate:PermitLimit", "2");
        builder.UseSetting("RateLimiting:Calculate:WindowSeconds", "60");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Products:PermitLimit"] = "2",
                ["RateLimiting:Products:WindowSeconds"] = "60",
                ["RateLimiting:Calculate:PermitLimit"] = "2",
                ["RateLimiting:Calculate:WindowSeconds"] = "60",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOpenFoodFactsClient>();
            services.AddTransient<IOpenFoodFactsClient>(sp =>
            {
                var httpClient = new HttpClient(new StubOpenFoodFactsHandler())
                {
                    BaseAddress = new Uri("https://world.openfoodfacts.org/")
                };
                return new OpenFoodFactsClient(
                    httpClient,
                    sp.GetRequiredService<ILogger<OpenFoodFactsClient>>());
            });
        });
    }
}

file sealed class StubOpenFoodFactsHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
