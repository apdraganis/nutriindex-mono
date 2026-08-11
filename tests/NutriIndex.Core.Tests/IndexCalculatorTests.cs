using NutriIndex.Core.Models;
using NutriIndex.Core.Services;

namespace NutriIndex.Core.Tests;

public class IndexCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsExpectedIndices()
    {
        var purchase = new Purchase(PriceEur: 2.50m, QuantityG: 500m);
        var nutrition = new NutritionPer100g(Kcal: 400m, ProteinG: 20m);

        var indices = IndexCalculator.Calculate(purchase, nutrition);

        Assert.Equal(0.125m, indices.EurPer100Kcal);
        Assert.Equal(0.25m, indices.EurPer10gProtein);
    }

    [Fact]
    public void Calculate_ThrowsWhenKcalIsZero()
    {
        var purchase = new Purchase(PriceEur: 2.50m, QuantityG: 500m);
        var nutrition = new NutritionPer100g(Kcal: 0m, ProteinG: 20m);

        Assert.Throws<ArgumentException>(() => IndexCalculator.Calculate(purchase, nutrition));
    }

    [Fact]
    public void Calculate_ThrowsWhenProteinIsZero()
    {
        var purchase = new Purchase(PriceEur: 2.50m, QuantityG: 500m);
        var nutrition = new NutritionPer100g(Kcal: 400m, ProteinG: 0m);

        Assert.Throws<ArgumentException>(() => IndexCalculator.Calculate(purchase, nutrition));
    }
}

public class QuantityParserTests
{
    [Theory]
    [InlineData("500 g", 500)]
    [InlineData("1,5 kg", null)]
    [InlineData("330 ml", 330)]
    [InlineData("400g", 400)]
    public void ParseGrams_ParsesKnownFormats(string input, int? expected)
    {
        var result = QuantityParser.ParseGrams(input);

        if (expected is null)
            Assert.Null(result);
        else
            Assert.Equal((decimal)expected, result);
    }
}
