using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Newtonsoft.Json;
using urlhandler.Extensions;
using urlhandler.Models;
using urlhandler.Services;
using urlhandler.ViewModels;
using urlhandler.Views;

namespace urlhandler.Helpers;

public static class WindowHelper {
  public static MainWindowViewModel? MainWindowViewModel { get; set; }
  public static MainWindow? MainWindow { get; set; }
  public static void Deactivate(MainWindowViewModel mainWindowView) {
    var minimized = mainWindowView is { isMinimizedByIdleTimer: false, mainWindow.WindowState: WindowState.Minimized };
    mainWindowView.mainWindow.ShowInTaskbar = !minimized;
    mainWindowView.isMinimizedByIdleTimer = minimized;
    if (mainWindowView.idleTimer == null) return;
    mainWindowView.idleTimer.IsEnabled = !minimized;
    if (minimized) mainWindowView.idleTimer.Stop();
    else mainWindowView.idleTimer.Start();
  }

  public static void Load(MainWindowViewModel mainWindowView) {
    new TrayService().InitializeTray(mainWindowView);
    if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10 ||
        Environment.OSVersion.Platform == PlatformID.Unix) {
      mainWindowView.notificationManager = Program.NotificationManager ?? throw new InvalidOperationException("Missing notification manager");
    }

    Task.Run(async () => {
      try {
        if (mainWindowView.args.Length > 0) {
          var path = AppDomain.CurrentDomain.BaseDirectory + "downloads.json";
          if (File.Exists(path) && !string.IsNullOrEmpty(File.ReadAllText(path))) {
            var data = File.ReadAllText(path);
            var downloads = JsonConvert.DeserializeObject<ObservableCollection<Downloads>>(data);

            if (downloads.Count > 0) {
              foreach (var download in downloads) {
                if (File.Exists(download.FilePath)) {
                  mainWindowView.DownloadedFiles.Insert(0, download);
                }
              }
              mainWindowView.HasFilesDownloaded = true;
            }
          }

          var parsedUrl = mainWindowView.args.First().ParseUrl();
          if (parsedUrl == null || parsedUrl == "invalid uri") {
            mainWindowView.Status = FeedbackHelper.InvalidUrl;
            await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
            return;
          }

          var authToken = parsedUrl.ExtractAuthToken();
          if (authToken == null) {
            mainWindowView.Status = FeedbackHelper.TokenFail;
            await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
            return;
          }

          mainWindowView.Url = parsedUrl;
          await ProcessHelper.HandleProcess(mainWindowView, parsedUrl);
        }
      }
      catch (Exception ex) {
        Debug.WriteLine(ex.Message);
      }
    });
    MinimizeWindowOnIdle();
  }

  private static void MinimizeWindowOnIdle() {
    try {
      var window = MainWindow!;

      var idleTimer = new DispatcherTimer {
        Interval = TimeSpan.FromSeconds(300)
      };
      idleTimer.Tick += async (sender, e) => {
        var elapsedTime = DateTime.Now - MainWindowViewModel!.lastInteractionTime;

        if (!(elapsedTime.TotalSeconds > 300)) return;
        MainWindowViewModel.isMinimizedByIdleTimer = true;
        window.WindowState = WindowState.Minimized;
        idleTimer.IsEnabled = false;
        idleTimer.Stop();
        await FeedbackHelper.ShowNotificationAsync(FeedbackHelper.Minimize, MainWindowViewModel);
        idleTimer.Start();
        window.PointerPressed += (sender, eventArgs) => ResetLastInteractionTime(MainWindowViewModel);
        window.PointerMoved += (sender, eventArgs) => ResetLastInteractionTime(MainWindowViewModel);
        window.KeyDown += (sender, eventArgs) => ResetLastInteractionTime(MainWindowViewModel);
        MainWindowViewModel.lastInteractionTime = DateTime.Now;
      };
    }
    catch (Exception ex) {
      Console.WriteLine($"Error in MinimizeWindowOnIdle: {ex.Message}");
      throw;
    }
  }

  private static void ResetLastInteractionTime(MainWindowViewModel mainWindowView) {
    mainWindowView.lastInteractionTime = DateTime.Now;

    if (!mainWindowView.isMinimizedByIdleTimer) {
      return;
    }

    mainWindowView.mainWindow.WindowState = WindowState.Normal;
    mainWindowView.mainWindow.ShowInTaskbar = true;
    mainWindowView.isMinimizedByIdleTimer = false;

    if (mainWindowView.idleTimer != null) {
      mainWindowView.idleTimer.IsEnabled = true;
      mainWindowView.idleTimer.Start();
    }
    else {
      Console.WriteLine("Error: idleTimer is null.");
    }
  }
  public static void ShowWindow() {
    if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopApp) return;
    var mainWindow = desktopApp.MainWindow;
    if (mainWindow == null) return;
    mainWindow.Show();
    mainWindow.WindowState = WindowState.Normal;
    mainWindow.ShowInTaskbar = true;
  }
}
