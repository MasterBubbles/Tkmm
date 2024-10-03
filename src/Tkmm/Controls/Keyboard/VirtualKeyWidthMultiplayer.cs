﻿using Avalonia.Data.Converters;
using System.Globalization;

namespace Tkmm.Controls.Keyboard;

public class VirtualKeyWidthMultiplayer : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = double.Parse(value.ToString());
        var p = double.Parse(parameter.ToString());
        return v * (p / 10.0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}