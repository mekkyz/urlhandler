using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using urlhandler.Helpers;
using urlhandler.ViewModels;
using Uri = System.Uri;

namespace urlhandler.Services.Concrete;
public interface ITrayService {
  void InitializeTray(MainWindowViewModel viewModel);
}

public class TrayService : ITrayService {
  internal TrayIcon? _notifyIcon;
  public void InitializeTray(MainWindowViewModel viewModel) {
    var _trayMenu = new NativeMenu {
            new NativeMenuItem {
                Header = "Maximize",
                Command = new RelayCommand(() => {
                    Dispatcher.UIThread.Invoke(() => {
                        viewModel.mainWindow.WindowState = WindowState.Maximized;
                        viewModel.mainWindow.ShowInTaskbar = true;
                    });
                })
            },
            new NativeMenuItem {
                Header = "Upload all edited files",
                Command = new RelayCommand(async () => {
                    await new UploadService().UploadFiles(viewModel, ignoreIndex: true);
                })
            },
            new NativeMenuItem {
                Header = "Exit",
                Command = new RelayCommand(() => {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp) {
                        desktopApp.Shutdown();
                    }
                })
            }
        };
        
    _notifyIcon = new TrayIcon {
      Icon = new(AssetLoader.Open(new Uri("avares://urlhandler/Assets/icon.ico"))),
      IsVisible = true,
      ToolTipText = "Url Handler",
      Menu = _trayMenu
    };
    // wire up events
    _notifyIcon.Clicked += (sender, e) => WindowHelper.ShowWindow();
  }
}
