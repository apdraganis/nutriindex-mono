using System.Globalization;
using System.Text.RegularExpressions;
using NutriIndex.Core.Models;

namespace NutriIndex.Core.Services;

public static partial class QuantityParser
{
    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(kg|g|ml|l)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuantityRegex();

    public static ParsedQuantity? Parse(string? quantity)
    {
        if (string.IsNullOrWhiteSpace(quantity))
            return null;

        var match = QuantityRegex().Match(quantity);
        if (!match.Success)
            return null;

        var valueText = match.Groups[1].Value.Replace(',', '.');
        if (!decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return null;

        var unit = match.Groups[2].Value.ToLowerInvariant();
        return unit switch
        {
            "kg" => new ParsedQuantity(value * 1000m, QuantityUnit.G),
            "g" => new ParsedQuantity(value, QuantityUnit.G),
            "l" => new ParsedQuantity(value * 1000m, QuantityUnit.Ml),
            "ml" => new ParsedQuantity(value, QuantityUnit.Ml),
            _ => null
        };
    }
}
