using NutriIndex.Core.Models;
using NutriIndex.Core.Services;

namespace NutriIndex.Core.Tests;

public class IndexCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsExpectedIndices_ForMass()
    {
        var purchase = new Purchase(PriceEur: 2.50m, Quantity: 500m, Unit: QuantityUnit.G);
        var nutrition = new NutritionFacts(Kcal: 400m, ProteinG: 20m);

        var indices = IndexCalculator.Calculate(purchase, nutrition);

        Assert.Equal(0.125m, indices.EurPer100Kcal);
        Assert.Equal(2.50m, indices.EurPer100gProtein);
    }

    [Fact]
    public void Calculate_ReturnsExpectedIndices_ForVolume()
    {
        var purchase = new Purchase(PriceEur: 1.20m, Quantity: 1000m, Unit: QuantityUnit.Ml);
        var nutrition = new NutritionFacts(Kcal: 42m, ProteinG: 3.4m);

        var indices = IndexCalculator.Calculate(purchase, nutrition);

        Assert.Equal(1.20m / 420m * 100m, indices.EurPer100Kcal);
        Assert.Equal(1.20m / 34m * 100m, indices.EurPer100gProtein);
    }

    [Fact]
    public void Calculate_ThrowsWhenKcalIsZero()
    {
        var purchase = new Purchase(PriceEur: 2.50m, Quantity: 500m, Unit: QuantityUnit.G);
        var nutrition = new NutritionFacts(Kcal: 0m, ProteinG: 20m);

        Assert.Throws<ArgumentException>(() => IndexCalculator.Calculate(purchase, nutrition));
    }

    [Fact]
    public void Calculate_ThrowsWhenProteinIsZero()
    {
        var purchase = new Purchase(PriceEur: 2.50m, Quantity: 500m, Unit: QuantityUnit.G);
        var nutrition = new NutritionFacts(Kcal: 400m, ProteinG: 0m);

        Assert.Throws<ArgumentException>(() => IndexCalculator.Calculate(purchase, nutrition));
    }
}

public class QuantityParserTests
{
    [Theory]
    [InlineData("500 g", 500, QuantityUnit.G)]
    [InlineData("1,5 kg", 1500, QuantityUnit.G)]
    [InlineData("400g", 400, QuantityUnit.G)]
    [InlineData("330 ml", 330, QuantityUnit.Ml)]
    [InlineData("1.5 L", 1500, QuantityUnit.Ml)]
    [InlineData("1 l", 1000, QuantityUnit.Ml)]
    public void Parse_ParsesKnownFormats(string input, int expectedValue, QuantityUnit expectedUnit)
    {
        var result = QuantityParser.Parse(input);

        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedUnit, result.Unit);
    }

    [Fact]
    public void Parse_ReturnsNullForUnknownFormat()
    {
        Assert.Null(QuantityParser.Parse("a handful"));
    }
}
