using Avalonia.Controls;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using urlhandler.ViewModels;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace urlhandler.Helpers {
  internal static class WindowHelper {
    internal static void Deactivate(MainWindowViewModel mainWindowView) {
      if (mainWindowView.isMinimizedByIdleTimer == false && mainWindowView.mainWindow.WindowState == WindowState.Minimized) {
        mainWindowView.mainWindow.ShowInTaskbar = false;
        if (mainWindowView.idleTimer != null) {
          mainWindowView.idleTimer.IsEnabled = false;
          mainWindowView.idleTimer.Stop();
        }
        mainWindowView.isMinimizedByIdleTimer = true;
      }
      else {
        mainWindowView.mainWindow.ShowInTaskbar = true;
        if (mainWindowView.idleTimer != null) {
          mainWindowView.idleTimer.IsEnabled = true;
          mainWindowView.idleTimer.Start();
        }
        mainWindowView.isMinimizedByIdleTimer = false;
      }
    }

    internal static void Load(MainWindowViewModel mainWindowView) {
      mainWindowView.InitializeSystemTrayIcon();

      if ((Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10) ||
          Environment.OSVersion.Platform == PlatformID.Unix) {
        mainWindowView.notificationManager = Program.NotificationManager ?? throw new InvalidOperationException("Missing notification manager");
      }

      Task.Run(async () => {
        await mainWindowView._tokenService.FetchAuthToken(mainWindowView);
        if (mainWindowView.args.Length > 0) {
          string pattern = @"^chemotion://\d+/(\w+\.\w+)/(\d+)$";
          Regex regex = new Regex(pattern);
          Match match = regex.Match(mainWindowView.args.First());

          if (!match.Success) {
            mainWindowView.Status = "Invalid URL format. Expected format: chemotion://3000/csii.png/12345";
            await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
            return;
          }

          string fileName = match.Groups[1].Value;
          string authToken = match.Groups[2].Value;

          string downloadUrl = $"http://localhost:3000/download?fileId={fileName}&authtoken={authToken}";

          mainWindowView._filePath = await mainWindowView._downloadService.DownloadFile(fileName, int.Parse(authToken), mainWindowView);
          if (mainWindowView._filePath == null) {
            mainWindowView.Status = "Failed to download file.";
            await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
            return;
          }

          mainWindowView.AddHistory(downloadUrl);
          mainWindowView._fileService.ProcessFile(mainWindowView._filePath, mainWindowView);
        }
      });

      mainWindowView.MinimizeWindowOnIdle();
    }

    internal static void ShowWindow() {
      if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp) {
        var mainWindow = desktopApp.MainWindow;
        if (mainWindow != null) {
          mainWindow.Show();
          mainWindow.WindowState = WindowState.Normal;
          mainWindow.ShowInTaskbar = true;
        }
      }
    }
  }
}
