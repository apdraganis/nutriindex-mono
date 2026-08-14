using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace NutriIndex.Api.Tests;

public class RateLimitingTests
{
    [Fact]
    public async Task Products_Returns429_AfterPermitLimit()
    {
        await using var factory = new NutriIndexApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");

        var first = await client.GetAsync("/api/products/1234567890123");
        var second = await client.GetAsync("/api/products/1234567890123");
        var third = await client.GetAsync("/api/products/1234567890123");

        Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);

        var body = await third.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Too many requests. Please try again later.", body.GetProperty("error").GetString());
        Assert.True(third.Headers.Contains("Retry-After") || third.Headers.RetryAfter is not null);
    }

    [Fact]
    public async Task Calculate_Returns429_AfterPermitLimit()
    {
        await using var factory = new NutriIndexApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.20");

        var payload = new
        {
            priceEur = 2.5m,
            quantityG = 500m,
            kcalPer100g = 400m,
            proteinPer100g = 20m
        };

        var first = await client.PostAsJsonAsync("/api/calculate", payload);
        var second = await client.PostAsJsonAsync("/api/calculate", payload);
        var third = await client.PostAsJsonAsync("/api/calculate", payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Health_IsNotRateLimited()
    {
        await using var factory = new NutriIndexApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.30");

        for (var i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Products_PartitionsByForwardedClientIp()
    {
        await using var factory = new NutriIndexApiFactory();
        var client = factory.CreateClient();

        async Task<HttpStatusCode> GetProductAsync(string ip)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/1234567890123");
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
            var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.NotFound, await GetProductAsync("198.51.100.1"));
        Assert.Equal(HttpStatusCode.NotFound, await GetProductAsync("198.51.100.1"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await GetProductAsync("198.51.100.1"));

        // A different client IP still has its own budget.
        Assert.Equal(HttpStatusCode.NotFound, await GetProductAsync("198.51.100.2"));
    }
}
