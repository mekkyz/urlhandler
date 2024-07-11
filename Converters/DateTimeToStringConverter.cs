using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace urlhandler.Converters;

public class DateTimeToStringConverter : IValueConverter {
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
    if (value is DateTime dateTime) {
      return dateTime.ToString("dd/MM/yyyy h:mm tt", culture);
    }
    else if (value is long epoch) {
      DateTime dateTimeFromEpoch = DateTimeOffset.FromUnixTimeSeconds(epoch).DateTime;

      DateTime localDateTime = dateTimeFromEpoch.ToLocalTime();

      if (targetType == typeof(IBrush)) {
        return DateTime.Now > localDateTime ? new SolidColorBrush(Color.FromRgb(231, 76, 60)) : new SolidColorBrush(Color.FromRgb(39, 174, 96));
      }
      return DateTime.Now < localDateTime ? "File upload will expire on " + localDateTime.ToString("dd/MM/yyyy h:mm tt", culture) : "File upload expired.";
    }
    return value!;
  }

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
    throw new NotImplementedException();
  }
}
