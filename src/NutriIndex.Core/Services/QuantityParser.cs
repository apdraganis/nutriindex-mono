using System.Globalization;
using System.Text.RegularExpressions;

namespace NutriIndex.Core.Services;

public static partial class QuantityParser
{
    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(g|ml)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuantityRegex();

    public static decimal? ParseGrams(string? quantity)
    {
        if (string.IsNullOrWhiteSpace(quantity))
            return null;

        var match = QuantityRegex().Match(quantity);
        if (!match.Success)
            return null;

        var valueText = match.Groups[1].Value.Replace(',', '.');
        if (!decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return null;

        return value;
    }
}
