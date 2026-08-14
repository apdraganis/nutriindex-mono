namespace NutriIndex.Core.Models;

public record CalculateRequest(
    decimal PriceEur,
    decimal Quantity,
    QuantityUnit Unit,
    decimal KcalPer100,
    decimal ProteinPer100);
