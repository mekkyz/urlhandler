using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using urlhandler.Helpers;
using urlhandler.Models;
using urlhandler.Services;
using urlhandler.Services.Concrete;
using urlhandler.Views;
using Timer = System.Timers;

namespace urlhandler.ViewModels;
public partial class
  MainWindowViewModel : ViewModelBase {
  internal HttpClient _httpClient = new HttpClient();
  internal string? _filePath;
  //internal string? _fileId;
  internal INotificationManager? notificationManager;
  internal DispatcherTimer? idleTimer = new DispatcherTimer();
  internal DateTime lastInteractionTime;
  internal bool isMinimizedByIdleTimer = false;
  internal MainWindow mainWindow;
  internal string[] args = { "", "" };
  internal System.Threading.Timer? _debounceTimer;
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
  [ObservableProperty] internal bool _hasFilesDownloaded = true;
  [ObservableProperty] internal bool _hasFilesEdited = true;
  [ObservableProperty] internal double _fileUpDownProgress = 0.0f;
  [ObservableProperty] internal string _fileUpDownProgressText = "";
  [ObservableProperty] internal string _url = "";
  [ObservableProperty] internal string _status = "";
  [ObservableProperty] internal ObservableCollection<string> _history = new ObservableCollection<string>();
  [ObservableProperty] internal bool _hasHistory = true;
  [ObservableProperty] internal int _selectedHistoryIndex = -1;
  [ObservableProperty] internal object? _selectedUrl;
  [ObservableProperty] internal bool _isAlreadyProcessing = false;
  [ObservableProperty] internal bool _isManualEnabled = false;
  [ObservableProperty] internal string _authToken = "";
  [ObservableProperty] private bool _isDarkMode = true;
  [ObservableProperty] private string _themeButtonIcon = "fa-solid fa-lightbulb";

  partial void OnIsDarkModeChanged(bool value) {
    ThemeButtonIcon = value ? "fa-solid fa-lightbulb" : "fa-regular fa-lightbulb";
    Application.Current!.RequestedThemeVariant = value == true ? ThemeVariant.Dark : ThemeVariant.Light;
  }

  public MainWindowViewModel(MainWindow mainWindow, string[] args) {

    this.mainWindow = mainWindow;
    this.args = args ?? throw new ArgumentNullException(nameof(args));

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
    mainWindow.Deactivated += (s, e) => WindowHelper.Deactivate(this);
  }

  [RelayCommand] public void AddHistory(string url) => _historyService.AddHistory(this, url);
  [RelayCommand] public void DeleteHistory() => _historyService.DeleteHistory(this);

  partial void OnSelectedHistoryIndexChanged(int value) => _historyService.IndexChange(this, value);

  private void MainWindow_Loaded(object? sender, EventArgs e) {
    WindowHelper.Load(this);
    Timer.Timer timer = new Timer.Timer(3000);
    timer.Elapsed += OnTimedEvent;
    timer.AutoReset = true;
    timer.Enabled = true;
    _historyService.LoadHistory(this);
  }

  private static void OnTimedEvent(object? source, ElapsedEventArgs e) {
    var downloadedFiles = WindowHelper.MainWindowViewModel?.DownloadedFiles;
    if (downloadedFiles != null) {
      for (int i = 0; i < downloadedFiles.Count; i++) {
        var file = downloadedFiles[i];
        if (file?.FilePath != null) {
          var lastWrite = File.GetLastWriteTime(file.FilePath);
          var creationTime = File.GetCreationTime(file.FilePath);
          var temp = WindowHelper.MainWindowViewModel?.EditedFiles.FirstOrDefault(x => x.FilePath == file.FilePath);

          if (lastWrite >= creationTime && lastWrite.Second != creationTime.Second) {
            if (temp != null) {
              temp.LastEdit = lastWrite;
            }
            else {
              WindowHelper.MainWindowViewModel?.EditedFiles.Add(new EditedFiles { FileName = file.FileName, FilePath = file.FilePath, FileTime = lastWrite, LastEdit = lastWrite, IsUpdated = true });
            }
          }
        }
      }
    }
  }

  [RelayCommand] public async Task Process() => await _processHelper.HandleProcess(this);
  [RelayCommand] public async Task<bool> UploadFiles() => await _uploadService.UploadFiles(this);
}
