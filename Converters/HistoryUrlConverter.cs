using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace urlhandler.Converters;

public class HistoryUrlConverter : IValueConverter {
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
    if (value is not string str) return value;
    var index = str.IndexOf('|');
    return index != -1 ? str[(index + 1)..] : value;
  }

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
    throw new NotImplementedException();
  }
}
