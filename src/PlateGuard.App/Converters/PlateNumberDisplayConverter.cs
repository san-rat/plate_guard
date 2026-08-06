using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PlateGuard.App.Converters;

// Plates are stored exactly as typed, so the same list can show "ABR1345" next to "rf4321".
// Display-only upper-casing keeps the column scannable without touching stored data.
public sealed class PlateNumberDisplayConverter : IValueConverter
{
    public static readonly PlateNumberDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string plate ? plate.Trim().ToUpperInvariant() : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Plate display formatting is one-way.");
    }
}
