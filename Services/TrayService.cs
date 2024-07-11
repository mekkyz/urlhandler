using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using urlhandler.Helpers;
using urlhandler.ViewModels;
using System;

namespace urlhandler.Services;

public interface ITrayService {
    void InitializeTray(MainWindowViewModel viewModel);
}

public class TrayService : ITrayService {
    private TrayIcon? _notifyIcon;
    private MainWindowViewModel? _mainWindowViewModel;

    public void InitializeTray(MainWindowViewModel viewModel) {
        _mainWindowViewModel = viewModel;

        var _trayMenu = new NativeMenu {
            new NativeMenuItem {
                Header = "Open app",
                Command = new RelayCommand(() => {
                    Dispatcher.UIThread.Invoke(() => {
                        _mainWindowViewModel!.mainWindow.WindowState = WindowState.Normal;
                        _mainWindowViewModel.mainWindow.ShowInTaskbar = true;
                    });
                })
            },
            new NativeMenuItem {
                Header = "Upload all & keep locally",
                Command = new AsyncRelayCommand(async () => await _mainWindowViewModel!.UploadFiles(""))
            },
            new NativeMenuItem {
                Header = "Upload all & delete locally",
                Command = new AsyncRelayCommand(async () => await _mainWindowViewModel!.UploadFiles("delete"))
            },
            new NativeMenuItem {
                Header = "Open files folder",
                Command = new RelayCommand(() => _mainWindowViewModel!.OpenDownloadDirectory())
            },
            new NativeMenuItem {
                Header = "Exit app",
                Command = new RelayCommand(() => {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp) {
                        desktopApp.Shutdown();
                    }
                })
            }
        };

        _notifyIcon = new TrayIcon {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://urlhandler/Assets/icon.ico"))),
            IsVisible = true,
            ToolTipText = "ChemotionURLHandler",
            Menu = _trayMenu
        };

        // wire up events
        _notifyIcon.Clicked += (sender, e) => WindowHelper.ShowWindow();
    }
}
