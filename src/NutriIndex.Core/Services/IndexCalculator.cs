using NutriIndex.Core.Models;

namespace NutriIndex.Core.Services;

public static class IndexCalculator
{
    public static Indices Calculate(Purchase purchase, NutritionPer100g nutrition)
    {
        var totalKcal = nutrition.Kcal * purchase.QuantityG / 100m;
        var totalProteinG = nutrition.ProteinG * purchase.QuantityG / 100m;

        if (totalKcal <= 0)
            throw new ArgumentException("Total kcal must be greater than zero.", nameof(nutrition));

        if (totalProteinG <= 0)
            throw new ArgumentException("Total protein must be greater than zero.", nameof(nutrition));

        return new Indices(
            EurPer100Kcal: purchase.PriceEur / totalKcal * 100m,
            EurPer10gProtein: purchase.PriceEur / totalProteinG * 10m);
    }

    public static Indices Calculate(CalculateRequest request) =>
        Calculate(
            new Purchase(request.PriceEur, request.QuantityG),
            new NutritionPer100g(request.KcalPer100g, request.ProteinPer100g));
}
