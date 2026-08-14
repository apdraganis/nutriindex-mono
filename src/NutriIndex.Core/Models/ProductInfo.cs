namespace NutriIndex.Core.Models;

public record ProductInfo(
    string Barcode,
    string Name,
    string? ImageUrl,
    decimal? DefaultQuantity,
    QuantityUnit? DefaultQuantityUnit,
    NutritionFacts? NutritionPer100g,
    NutritionFacts? NutritionPer100ml);
