using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using urlhandler.Services;
using urlhandler.ViewModels;
using urlhandler.Views;

namespace urlhandler.Helpers;

public static partial class WindowHelper {
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
      if (mainWindowView.args.Length > 0) {
        var match = UrlProtocolRegex().Match(mainWindowView.args.First());
        if (!match.Success) {
          mainWindowView.Status = "Invalid URL format.";
          await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return;
        }
        var encodedUrl = match.Groups[1].Value;
        var decodedUrl = HttpUtility.UrlDecode(encodedUrl);
        var authToken = decodedUrl[(decodedUrl.LastIndexOf('/') + 1)..];
        var downloadUrl = ApiHelper.DownloadUrl(authToken);
        mainWindowView._filePath = await mainWindowView._downloadService.DownloadFile(mainWindowView, authToken);
        if (mainWindowView._filePath == null) {
          mainWindowView.Status = "Failed to download file.";
          await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return;
        }

        if (mainWindowView._filePath == "alreadyExists") {
          mainWindowView.Status = "Same file or named already being processed.";
          await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return;
        }
        mainWindowView.AddHistory(downloadUrl);
        await mainWindowView._fileService.ProcessFile(mainWindowView._filePath, mainWindowView);
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
        await NotificationHelper.ShowNotificationAsync("Minimized due to inactivity.", MainWindowViewModel, "Auto-minimize");
      };
      idleTimer.Start();
      window.PointerPressed += (sender, eventArgs) => InteractionHelper.ResetLastInteractionTime(MainWindowViewModel!);
      window.PointerMoved += (sender, eventArgs) => InteractionHelper.ResetLastInteractionTime(MainWindowViewModel!);
      window.KeyDown += (sender, eventArgs) => InteractionHelper.ResetLastInteractionTime(MainWindowViewModel!);
      MainWindowViewModel!.lastInteractionTime = DateTime.Now;
    }

    catch (Exception ex) {
      Console.WriteLine($"Error in MinimizeWindowOnIdle: {ex.Message}");
      throw;
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

  [GeneratedRegex(@"^chemotion:\/\/\?url=(http%3A%2F%2F[\w.:%\/=?&-]+)$")]
  private static partial Regex UrlProtocolRegex();
}
