using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NutriIndex.Api.Services;
using NutriIndex.Core.Models;

namespace NutriIndex.Api.Tests;

public class ProductLookupServiceTests
{
    [Fact]
    public async Task ConcurrentLookups_ForSameBarcode_CallOpenFoodFactsOnce()
    {
        var off = new FakeOpenFoodFactsClient
        {
            Delay = TimeSpan.FromMilliseconds(50),
            Product = SampleProduct("123")
        };
        var sut = CreateSut(off);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(_ => sut.GetProductAsync("123")));

        Assert.Equal(1, off.Calls);
        Assert.All(results, product => Assert.Equal("123", product?.Barcode));
    }

    [Fact]
    public async Task CacheHit_DoesNotCallOpenFoodFactsAgain()
    {
        var off = new FakeOpenFoodFactsClient { Product = SampleProduct("123") };
        var sut = CreateSut(off);

        await sut.GetProductAsync("123");
        await sut.GetProductAsync("123");

        Assert.Equal(1, off.Calls);
    }

    [Fact]
    public async Task MissingProduct_IsCached_AndDoesNotCallOpenFoodFactsAgain()
    {
        var off = new FakeOpenFoodFactsClient { Product = null };
        var sut = CreateSut(off);

        var first = await sut.GetProductAsync("000");
        var second = await sut.GetProductAsync("000");

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, off.Calls);
    }

    [Fact]
    public async Task DifferentBarcodes_AreFetchedIndependently()
    {
        var off = new FakeOpenFoodFactsClient
        {
            Delay = TimeSpan.FromMilliseconds(30),
            ProductFactory = barcode => SampleProduct(barcode)
        };
        var sut = CreateSut(off);

        var results = await Task.WhenAll(
            sut.GetProductAsync("111"),
            sut.GetProductAsync("222"));

        Assert.Equal(2, off.Calls);
        Assert.Equal(new[] { "111", "222" }, results.Select(p => p?.Barcode).OrderBy(b => b));
    }

    [Fact]
    public async Task FailedFetch_IsNotCached_AndCanBeRetried()
    {
        var off = new FakeOpenFoodFactsClient
        {
            FailuresBeforeSuccess = 1,
            Product = SampleProduct("123")
        };
        var sut = CreateSut(off);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetProductAsync("123"));
        var product = await sut.GetProductAsync("123");

        Assert.Equal(2, off.Calls);
        Assert.Equal("123", product?.Barcode);
    }

    private static ProductLookupService CreateSut(IOpenFoodFactsClient client)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caching:Products:PositiveTtlSeconds"] = "86400",
                ["Caching:Products:NegativeTtlSeconds"] = "300",
            })
            .Build();

        return new ProductLookupService(
            client,
            new MemoryCache(new MemoryCacheOptions()),
            configuration);
    }

    private static ProductInfo SampleProduct(string barcode) =>
        new(barcode, "Test product", null, 100, new NutritionPer100g(200, 10));

    private sealed class FakeOpenFoodFactsClient : IOpenFoodFactsClient
    {
        public int Calls;
        public TimeSpan Delay = TimeSpan.Zero;
        public ProductInfo? Product;
        public Func<string, ProductInfo?>? ProductFactory;
        public int FailuresBeforeSuccess;

        public async Task<ProductInfo?> GetProductAsync(string barcode, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);

            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, CancellationToken.None);

            if (Interlocked.Decrement(ref FailuresBeforeSuccess) >= 0)
                throw new HttpRequestException("Open Food Facts unavailable");

            return ProductFactory is not null ? ProductFactory(barcode) : Product;
        }
    }
}
