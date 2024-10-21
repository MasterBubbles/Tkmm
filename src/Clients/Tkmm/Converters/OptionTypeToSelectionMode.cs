﻿using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Tkmm.Abstractions;

namespace Tkmm.Converters;

public class OptionTypeToSelectionMode : IValueConverter
{
    public static OptionTypeToSelectionMode Shared { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch {
            OptionGroupType.Multi => SelectionMode.Multiple | SelectionMode.Toggle,
            OptionGroupType.MultiRequired => SelectionMode.Multiple | SelectionMode.Toggle | SelectionMode.AlwaysSelected,
            OptionGroupType.Single => SelectionMode.Single | SelectionMode.Toggle,
            OptionGroupType.SingleRequired => SelectionMode.Single | SelectionMode.Toggle | SelectionMode.AlwaysSelected,
            _ => null
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}