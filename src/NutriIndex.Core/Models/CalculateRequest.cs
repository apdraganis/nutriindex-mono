namespace NutriIndex.Core.Models;

public record CalculateRequest(
    decimal PriceEur,
    decimal QuantityG,
    decimal KcalPer100g,
    decimal ProteinPer100g);
