using System.Globalization;

namespace POS.Application.Common;

public static class ItemCodeGenerator
{
    private const int PadWidth = 5;
    private const int MaxCountedDigits = 9;

    public static string Next(IEnumerable<string> existingCodes)
    {
        var max = 0;
        foreach (var code in existingCodes)
        {
            var trimmed = code?.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxCountedDigits) continue;
            if (!trimmed.All(char.IsAsciiDigit)) continue;

            var n = int.Parse(trimmed, CultureInfo.InvariantCulture);
            if (n > max) max = n;
        }

        return (max + 1).ToString(CultureInfo.InvariantCulture).PadLeft(PadWidth, '0');
    }
}
