namespace NutriIndex.Core.Models;

public record ProductInfo(
    string Barcode,
    string Name,
    string? ImageUrl,
    decimal? DefaultQuantityG,
    NutritionPer100g? Nutrition);
