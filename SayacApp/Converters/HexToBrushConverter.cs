using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SayacApp.Converters;

/// <summary>Converts a 6-digit hex string ("1A1A1A") into a brush for the swatches.</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = (value as string ?? "").Replace("#", "");
        try { return new SolidColorBrush(Color.Parse("#" + hex)); }
        catch { return Brushes.Transparent; }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
