using System;
using Avalonia;
using DesktopNotifications;
using DesktopNotifications.Avalonia;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace urlhandler {
  internal class Program {
    public static INotificationManager NotificationManager = null!;

    private static void Main(string[] args) {
      BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() {
      IconProvider.Current.Register<FontAwesomeIconProvider>();

      if (
          Environment.OSVersion.Platform == PlatformID.Win32NT
              && Environment.OSVersion.Version.Major >= 10
          || Environment.OSVersion.Platform == PlatformID.Unix
      ) {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .SetupDesktopNotifications(out NotificationManager!)
            .LogToTrace();
      }
      else {
        return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
      }
    }
  }
}
