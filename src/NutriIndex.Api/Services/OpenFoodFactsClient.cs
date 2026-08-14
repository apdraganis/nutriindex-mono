using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using NutriIndex.Core.Models;
using NutriIndex.Core.Services;

namespace NutriIndex.Api.Services;

public interface IOpenFoodFactsClient
{
    Task<ProductInfo?> GetProductAsync(string barcode, CancellationToken cancellationToken = default);
}

public class OpenFoodFactsClient : IOpenFoodFactsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsClient> _logger;

    public OpenFoodFactsClient(HttpClient httpClient, ILogger<OpenFoodFactsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductInfo?> GetProductAsync(string barcode, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v2/product/{barcode}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<OffProductResponse>(stream, JsonOptions, cancellationToken);

        if (payload?.Status != 1 || payload.Product is null)
            return null;

        var product = payload.Product;
        var parsedQuantity = QuantityParser.Parse(product.Quantity);

        return new ProductInfo(
            Barcode: barcode,
            Name: product.ProductName ?? product.GenericName ?? "Unknown product",
            ImageUrl: product.ImageUrl,
            DefaultQuantity: parsedQuantity?.Value,
            DefaultQuantityUnit: parsedQuantity?.Unit,
            NutritionPer100g: TryGetNutrition(
                product.Nutriments?.EnergyKcal100g,
                product.Nutriments?.EnergyKj100g,
                product.Nutriments?.Proteins100g),
            NutritionPer100ml: TryGetNutrition(
                product.Nutriments?.EnergyKcal100ml,
                product.Nutriments?.EnergyKj100ml,
                product.Nutriments?.Proteins100ml));
    }

    private static NutritionFacts? TryGetNutrition(decimal? kcal, decimal? energyKj, decimal? protein)
    {
        if (kcal is null or <= 0 && energyKj is > 0)
            kcal = energyKj / 4.184m;

        if (kcal is null or <= 0 || protein is null or <= 0)
            return null;

        return new NutritionFacts(kcal.Value, protein.Value);
    }

    private sealed class OffProductResponse
    {
        public int Status { get; init; }
        public OffProduct? Product { get; init; }
    }

    private sealed class OffProduct
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; init; }

        [JsonPropertyName("generic_name")]
        public string? GenericName { get; init; }

        [JsonPropertyName("quantity")]
        public string? Quantity { get; init; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; init; }

        public OffNutriments? Nutriments { get; init; }
    }

    private sealed class OffNutriments
    {
        [JsonPropertyName("energy-kcal_100g")]
        public decimal? EnergyKcal100g { get; init; }

        [JsonPropertyName("energy-kj_100g")]
        public decimal? EnergyKj100g { get; init; }

        [JsonPropertyName("proteins_100g")]
        public decimal? Proteins100g { get; init; }

        [JsonPropertyName("energy-kcal_100ml")]
        public decimal? EnergyKcal100ml { get; init; }

        [JsonPropertyName("energy-kj_100ml")]
        public decimal? EnergyKj100ml { get; init; }

        [JsonPropertyName("proteins_100ml")]
        public decimal? Proteins100ml { get; init; }
    }
}
