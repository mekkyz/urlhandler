using System;
using DesktopNotifications;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using urlhandler.ViewModels;

namespace urlhandler.Helpers;

internal class NotificationHelper {
  internal async Task ShowNotificationAsync(string body, MainWindowViewModel mainWindowView, string title = "Status") {
    try {
      string detailedBody = body;
      if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10
          || Environment.OSVersion.Platform == PlatformID.Unix) {
        if (body.StartsWith("Failed to download")) {
          detailedBody += " Please check your network connection and ensure the file ID and authentication token are correct.";
        }
        else if (body.StartsWith("Invalid URL format")) {
          detailedBody += " Ensure the URL matches the expected pattern.";
        }
        else if (body.StartsWith("File access error")) {
          detailedBody += " Make sure the file is not being used by another application and you have sufficient permissions.";
        }
        Notification nf = new Notification {
          Title = title,
          Body = detailedBody,
          BodyImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico")
        };
        await mainWindowView.notificationManager!.ShowNotification(nf);
      }
    }
    catch (Exception ex) {
      Debug.WriteLine($"Error showing notification: {ex.Message}");
    }
  }
}
