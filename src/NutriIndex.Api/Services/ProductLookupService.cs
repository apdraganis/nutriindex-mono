using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using NutriIndex.Core.Models;

namespace NutriIndex.Api.Services;

public sealed class ProductLookupService
{
    private readonly IOpenFoodFactsClient _client;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, Lazy<Task<ProductInfo?>>> _inflight = new();

    public ProductLookupService(
        IOpenFoodFactsClient client,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _client = client;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<ProductInfo?> GetProductAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:{barcode}";

        if (_cache.TryGetValue(cacheKey, out ProductCacheEntry? cached) && cached is not null)
            return cached.Product;

        var lazy = _inflight.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task<ProductInfo?>>(
                () => FetchAndCacheAsync(cacheKey, barcode),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await lazy.Value.WaitAsync(cancellationToken);
        }
        finally
        {
            _inflight.TryRemove(KeyValuePair.Create(cacheKey, lazy));
        }
    }

    private async Task<ProductInfo?> FetchAndCacheAsync(string cacheKey, string barcode)
    {
        var product = await _client.GetProductAsync(barcode);

        var ttlSeconds = product is null
            ? _configuration.GetValue("Caching:Products:NegativeTtlSeconds", 300)
            : _configuration.GetValue("Caching:Products:PositiveTtlSeconds", 86_400);

        _cache.Set(cacheKey, new ProductCacheEntry(product), TimeSpan.FromSeconds(ttlSeconds));
        return product;
    }

    private sealed record ProductCacheEntry(ProductInfo? Product);
}
