using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using urlhandler.Services.Concrete;
using urlhandler.ViewModels;
using urlhandler.Views;

namespace urlhandler.Helpers;
public static class WindowHelper {
  public static MainWindowViewModel? MainWindowViewModel { get; set; }
  public static MainWindow? MainWindow { get; set; }
  public static void Deactivate(MainWindowViewModel mainWindowView) {
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

  public static void Load(MainWindowViewModel mainWindowView) {
    new TrayService().InitializeTray(mainWindowView);
    if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10 ||
        Environment.OSVersion.Platform == PlatformID.Unix) {
      mainWindowView.notificationManager = Program.NotificationManager ?? throw new InvalidOperationException("Missing notification manager");
    }

    Task.Run(async () => {
      //await mainWindowView._tokenService.FetchAuthToken(mainWindowView);
      if (mainWindowView.args.Length > 0) {
        var pattern = @"^chemotion:\/\/\?url=(http%3A%2F%2F[\w.:%\/=?&-]+)$";
        var regex = new Regex(pattern);
        var match = regex.Match(mainWindowView.args.First());

        if (!match.Success) {
          mainWindowView.Status = "Invalid URL format.";
          await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return;
        }

        string encodedUrl = match.Groups[1].Value;
        string decodedUrl = HttpUtility.UrlDecode(encodedUrl);
        var authToken = decodedUrl[(decodedUrl.LastIndexOf('/') + 1)..] ?? "";

        var downloadUrl = ApiHelper.DownloadUrl(authToken);

        mainWindowView._filePath = await mainWindowView._downloadService.DownloadFile(mainWindowView, authToken);
        if (mainWindowView._filePath == null) {
          mainWindowView.Status = "Failed to download file.";
          await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return;
        }

        mainWindowView.AddHistory(downloadUrl);
        mainWindowView._fileService.ProcessFile(mainWindowView._filePath, mainWindowView);
      }
    });

    MinimizeWindowOnIdle();
  }

  public static void MinimizeWindowOnIdle() {
    try {
      var idleTimer = MainWindowViewModel!.idleTimer;
      var window = MainWindow!;

      idleTimer = new DispatcherTimer {
        Interval = TimeSpan.FromSeconds(300)
      };
      idleTimer.Tick += async (sender, e) => {
        var elapsedTime = DateTime.Now - MainWindowViewModel.lastInteractionTime;
        
        if (elapsedTime.TotalSeconds > 300) {
          MainWindowViewModel.isMinimizedByIdleTimer = true;
          window.WindowState = WindowState.Minimized;
          idleTimer.IsEnabled = false;
          idleTimer.Stop();
          await MainWindowViewModel._notificationHelper.ShowNotificationAsync("Minimized due to inactivity.", MainWindowViewModel, "Auto-minimize");
        }
      };
      idleTimer.Start();
      window.PointerPressed += (sender, eventArgs) => InteractionHelper.ResetLastInteractionTime(MainWindowViewModel);
      window.PointerMoved += (sender, eventArgs) => InteractionHelper.ResetLastInteractionTime(MainWindowViewModel);
      window.KeyDown += (sender, eventArgs) => InteractionHelper.ResetLastInteractionTime(MainWindowViewModel);
      MainWindowViewModel.lastInteractionTime = DateTime.Now;
    }

    catch (Exception ex) {
      Console.WriteLine($"Error in MinimizeWindowOnIdle: {ex.Message}");
      throw;
    }
  }

  public static void ShowWindow() {
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
