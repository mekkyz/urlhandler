using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using urlhandler.Models;
using urlhandler.Services.Concrete;
using urlhandler.Helpers;

namespace urlhandler.ViewModels {
  public partial class MainWindowViewModel : ViewModelBase {
    internal TrayIcon? _notifyIcon;
    internal HttpClient _httpClient = new HttpClient();
    internal string? _filePath;
    internal string? _fileId;
    internal INotificationManager? notificationManager;
    internal DispatcherTimer? idleTimer;
    internal DateTime lastInteractionTime;
    internal bool isMinimizedByIdleTimer = false;
    internal MainWindow mainWindow;
    internal string[] args = { "", "" };


    internal Timer? _debounceTimer;
    internal ByteService _byteService;
    internal DownloadService _downloadService;
    internal UpdateService _updateService;
    internal TokenService _tokenService;
    internal UploadService _uploadService;
    internal HistoryService _historyService;
    internal FileService _fileService;
    internal NotificationHelper _notificationHelper;
    internal ProcessHelper _processHelper;

    internal Process? _fileProcess;


    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasFilesDownloaded))] internal ObservableCollection<Downloads> _downloadedFiles = new ObservableCollection<Downloads>();
    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasFilesEdited))] internal ObservableCollection<EditedFiles> _editedFiles = new ObservableCollection<EditedFiles>();
    [ObservableProperty] internal int _selectedDownloadedFileIndex = -1;
    [ObservableProperty] internal int _selectedEditedFileIndex = -1;
    [ObservableProperty] internal bool _hasFilesDownloaded = !false;
    [ObservableProperty] internal bool _hasFilesEdited = !false;
    [ObservableProperty] internal double _fileUpDownProgress = 0.0f;
    [ObservableProperty] internal string _fileUpDownProgressText = "";
    [ObservableProperty] internal string _url = "";
    [ObservableProperty] internal string _status = "";
    [ObservableProperty] internal ObservableCollection<string> _history = new ObservableCollection<string>();
    [ObservableProperty] internal bool _hasHistory = !false;
    [ObservableProperty] internal int _selectedHistoryIndex = -1;
    [ObservableProperty] internal object? _selectedUrl;
    [ObservableProperty] internal bool _isAlreadyProcessing = false;
    [ObservableProperty] internal bool _isManualEnabled = false;
    [ObservableProperty] internal int? _authToken;

    public MainWindowViewModel(MainWindow mainWindow, string[] args) {
      this.mainWindow = mainWindow;
      this.args = args ?? throw new ArgumentNullException(nameof(args));


      _byteService = new ByteService();
      _downloadService = new DownloadService();
      _updateService = new UpdateService();
      _tokenService = new TokenService();
      _uploadService = new UploadService();
      _historyService = new HistoryService();
      _fileService = new FileService();
      _notificationHelper = new NotificationHelper();
      _processHelper = new ProcessHelper();

      SetupEventHandlers();

    }

    private void SetupEventHandlers() {
      mainWindow.Loaded += MainWindow_Loaded;
      mainWindow.Deactivated += MainWindow_Deactivated;
    }

    [RelayCommand]
    public void AddHistory(string url) {
      _historyService.AddHistory(this, url);
    }

    [RelayCommand]
    public void DeleteHistory() {
      _historyService.DeleteHistory(this);
    }

    partial void OnSelectedHistoryIndexChanged(int value) {
      _historyService.IndexChange(this, value);
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e) {
      WindowHelper.Deactivate(this);
    }

    private void MainWindow_Loaded(object? sender, EventArgs e) {
      WindowHelper.Load(this);
      _historyService.LoadHistory(this);
    }

    [RelayCommand]
    public async Task Process() {
      await _processHelper.HandleProcess(this);
    }

    public async Task<bool> UploadFiles(MainWindowViewModel mainWindowView, bool ignoreIndex = false) {
      return await _uploadService.UploadFiles(this, ignoreIndex);
    }

    [RelayCommand]
    public async Task<bool> UploadFiles() {
      return await _uploadService.UploadFiles(this);
    }

    private void ResetLastInteractionTime() {
      InteractionHelper.ResetLastInteractionTime(this);
    }

    internal void MinimizeWindowOnIdle() {
      try {
        var window = mainWindow;
        idleTimer = new DispatcherTimer();
        idleTimer.Interval = TimeSpan.FromSeconds(300);
        idleTimer.Tick += async (sender, e) => {
          var elapsedTime = DateTime.Now - lastInteractionTime;
          if (elapsedTime.TotalSeconds > 300) {
            isMinimizedByIdleTimer = true;
            mainWindow.WindowState = WindowState.Minimized;
            idleTimer.IsEnabled = false;
            idleTimer.Stop();
            await _notificationHelper.ShowNotificationAsync("The window has been minimized due to inactivity. Move the mouse or press any key to restore.", this, "Auto-minimize");
          }
        };
        idleTimer.Start();
        window.PointerPressed += Window_PointerPressed;
        window.PointerMoved += Window_PointerMoved;
        window.KeyDown += Window_KeyDown;
        lastInteractionTime = DateTime.Now;
      }
      catch (Exception ex) {
        Console.WriteLine($"Error in MinimizeWindowOnIdle: {ex.Message}");
        throw;
      }
    }

    internal void InitializeSystemTrayIcon() {
      var _trayMenu = new NativeMenu();
      _trayMenu.Add(
          new NativeMenuItem {
            Header = "Maximize",
            Command = new RelayCommand(() => {
              Dispatcher.UIThread.Invoke(() => {
                mainWindow.WindowState = WindowState.Maximized;
                mainWindow.ShowInTaskbar = true;
              });
            })
          }
      );
      _trayMenu.Add(
          new NativeMenuItem {
            Header = "Upload all edited files",
            Command = new RelayCommand(async () => {
              await UploadFiles(this, ignoreIndex: true);
            })
          }
      );

      _trayMenu.Add(
          new NativeMenuItem {
            Header = "Exit",
            Command = new RelayCommand(() => {
              if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp) {
                desktopApp.Shutdown();
              }
            })
          }
      );

      _notifyIcon = new TrayIcon {
        Icon = new(AppDomain.CurrentDomain.BaseDirectory + "icon.ico"),
        IsVisible = true,
        ToolTipText = "Url Handler",
        Menu = _trayMenu
      };
      // wire up events
      _notifyIcon.Clicked += (sender, e) => ShowMainWindow();
    }

    private void ShowMainWindow() {
      WindowHelper.ShowWindow();
    }

    #region events
    private void Window_KeyDown(object? sender, KeyEventArgs e) {
      // reset interaction on key press
      ResetLastInteractionTime();
    }
    private void Window_PointerMoved(object? sender, PointerEventArgs e) {
      // reset interaction on mouse move
      ResetLastInteractionTime();
    }
    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e) {
      // reset interaction on mouse click
      ResetLastInteractionTime();
    }
    private void OnActivated(object sender, EventArgs e) {
      // reset interaction on window focus
      ResetLastInteractionTime();
    }
    #endregion events

  }
}
