using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using DesktopNotifications.FreeDesktop;
using DesktopNotifications.Windows;
using System;

namespace DesktopNotifications.Avalonia {

  public static class AppBuilderExtensions {
    public static AppBuilder SetupDesktopNotifications(this AppBuilder builder, out INotificationManager? manager) {
      if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10) {
        var context = WindowsApplicationContext.FromCurrentProcess();
        manager = new WindowsNotificationManager(context);
      }
      else if (Environment.OSVersion.Platform == PlatformID.Unix) {
        var context = FreeDesktopApplicationContext.FromCurrentProcess();
        manager = new FreeDesktopNotificationManager(context);
      }
      else {
        // todo: macOS once implemented/stable
        manager = null;
        return builder;
      }

      // todo: any better way of doing this?
      manager.Initialize().GetAwaiter().GetResult();

      var manager_ = manager;
      builder.AfterSetup(b => {
        if (b.Instance?.ApplicationLifetime is IControlledApplicationLifetime lifetime) {
          lifetime.Exit += (s, e) => { manager_.Dispose(); };
        }
      });

      return builder;
    }
  }
}
