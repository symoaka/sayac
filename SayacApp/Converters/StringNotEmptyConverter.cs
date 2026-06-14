using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SayacApp.Converters;

/// <summary>
/// Returns true when the bound string is non-empty. Pass ConverterParameter="invert"
/// to flip it (true when empty) — used to swap an emoji vs. a fallback initial in a chip.
/// </summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var has = !string.IsNullOrWhiteSpace(value as string);
        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);
        return invert ? !has : has;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
