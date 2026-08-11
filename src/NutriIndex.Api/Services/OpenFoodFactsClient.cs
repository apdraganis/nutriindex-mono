using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using NutriIndex.Core.Models;
using NutriIndex.Core.Services;

namespace NutriIndex.Api.Services;

public class OpenFoodFactsClient
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
        var nutrition = TryGetNutrition(product.Nutriments);

        return new ProductInfo(
            Barcode: barcode,
            Name: product.ProductName ?? product.GenericName ?? "Unknown product",
            ImageUrl: product.ImageUrl,
            DefaultQuantityG: QuantityParser.ParseGrams(product.Quantity),
            Nutrition: nutrition);
    }

    private static NutritionPer100g? TryGetNutrition(OffNutriments? nutriments)
    {
        if (nutriments is null)
            return null;

        var kcal = nutriments.EnergyKcal100g;
        if (kcal is null or <= 0 && nutriments.EnergyKj100g is > 0)
            kcal = nutriments.EnergyKj100g / 4.184m;

        var protein = nutriments.Proteins100g;

        if (kcal is null or <= 0 || protein is null or <= 0)
            return null;

        return new NutritionPer100g(kcal.Value, protein.Value);
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
    }
}
