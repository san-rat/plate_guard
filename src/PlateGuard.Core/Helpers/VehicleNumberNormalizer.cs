using System.Text;

namespace PlateGuard.Core.Helpers;

public static class VehicleNumberNormalizer
{
    public static string Normalize(string? vehicleNumber)
    {
        if (string.IsNullOrWhiteSpace(vehicleNumber))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(vehicleNumber.Length);

        // Keep only letters and digits so format variations map to one business key.
        foreach (var character in vehicleNumber.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                normalized.Append(character);
            }
        }

        return normalized.ToString();
    }
}
