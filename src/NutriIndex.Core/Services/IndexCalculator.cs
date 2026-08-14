using NutriIndex.Core.Models;

namespace NutriIndex.Core.Services;

public static class IndexCalculator
{
    public static Indices Calculate(Purchase purchase, NutritionFacts nutrition)
    {
        var totalKcal = nutrition.Kcal * purchase.Quantity / 100m;
        var totalProteinG = nutrition.ProteinG * purchase.Quantity / 100m;

        if (totalKcal <= 0)
            throw new ArgumentException("Total kcal must be greater than zero.", nameof(nutrition));

        if (totalProteinG <= 0)
            throw new ArgumentException("Total protein must be greater than zero.", nameof(nutrition));

        return new Indices(
            EurPer100Kcal: purchase.PriceEur / totalKcal * 100m,
            EurPer100gProtein: purchase.PriceEur / totalProteinG * 100m);
    }

    public static Indices Calculate(CalculateRequest request) =>
        Calculate(
            new Purchase(request.PriceEur, request.Quantity, request.Unit),
            new NutritionFacts(request.KcalPer100, request.ProteinPer100));
}
