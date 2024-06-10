using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using urlhandler.Extensions;
using urlhandler.Helpers;
using urlhandler.Models;
using urlhandler.Services;
using urlhandler.Views;
using Timer = System.Timers;

namespace urlhandler.ViewModels;

public partial class
  MainWindowViewModel : ObservableObject {
  internal readonly HttpClient _httpClient = new HttpClient();
  internal string? _filePath;
  internal INotificationManager? notificationManager;
  internal readonly DispatcherTimer? idleTimer = new DispatcherTimer();
  internal DateTime lastInteractionTime;
  internal bool isMinimizedByIdleTimer = false;
  internal readonly MainWindow mainWindow;
  internal readonly string[] args;
  internal readonly DownloadService _downloadService;
  internal readonly UploadService _uploadService;
  private readonly HistoryService _historyService;
  internal readonly FileService _fileService;
  internal Process? _fileProcess;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasFilesDownloaded))]
  ObservableCollection<Downloads> _downloadedFiles = [];

  [ObservableProperty] private ObservableCollection<float> _editedFileIds = [];
  [ObservableProperty] int _selectedDownloadedFileIndex = -1;
  [ObservableProperty] bool _hasFilesDownloaded;
  [ObservableProperty] private double _fileUpDownProgress;
  [ObservableProperty] string? _fileUpDownProgressText = "";
  [ObservableProperty] string _url = "";
  [ObservableProperty] string? _status = "";
  [ObservableProperty] ObservableCollection<string> _history = new ObservableCollection<string>();
  [ObservableProperty] bool _hasHistory;
  [ObservableProperty] int _selectedHistoryIndex = -1;
  [ObservableProperty] object? _selectedUrl;
  [ObservableProperty] private bool _isAlreadyProcessing;
  [ObservableProperty] private bool _isManualEnabled;
  [ObservableProperty] string _authToken = "";
  [ObservableProperty] private bool _isDarkMode = true;
  [ObservableProperty] private string _themeButtonIcon = "fa-solid fa-lightbulb";

  partial void OnIsDarkModeChanged(bool value) {
    ThemeButtonIcon = value ? "fa-solid fa-lightbulb" : "fa-regular fa-lightbulb";
    Application.Current!.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
  }

  public MainWindowViewModel(MainWindow mainWindow, string[] args) {
    this.mainWindow = mainWindow;
    this.args = args ?? throw new ArgumentNullException(nameof(args));

    _downloadService = new DownloadService();
    _uploadService = new UploadService();
    _historyService = new HistoryService();
    _fileService = new FileService();
    Process = new RelayCommand<Task>(_ => Task.Run(async () => await ProcessCommand()));
    SetupEventHandlers();
  }

  private void SetupEventHandlers() {
    mainWindow.Loaded += MainWindow_Loaded;
    mainWindow.Deactivated += (s, e) => WindowHelper.Deactivate(this);
  }

  [RelayCommand]
  public void AddHistory(string url) => _historyService.AddHistory(this, url);

  [RelayCommand]
  public void DeleteHistory() => _historyService.DeleteHistory(this);

  partial void OnSelectedHistoryIndexChanged(int value) => _historyService.IndexChange(this, value);

  private void MainWindow_Loaded(object? sender, EventArgs e) {
    WindowHelper.Load(this);
    var timer = new Timer.Timer(1);
    timer.Elapsed += OnTimedEvent;
    timer.Enabled = true;
    _historyService.LoadHistory(this);
  }

  private void OnTimedEvent(object? source, Timer.ElapsedEventArgs e) {
    try {
      var downloadedFiles = WindowHelper.MainWindowViewModel?.DownloadedFiles;
      if (downloadedFiles == null) return;

      foreach (var file in downloadedFiles) {
        var fileSumOnDisk = file.FilePath.FileCheckSum();
        var fileSumOnDownload = file.FileSumOnDownload;

        if (!fileSumOnDisk.Equals(fileSumOnDownload)) {
          if (WindowHelper.MainWindowViewModel != null && !WindowHelper.MainWindowViewModel.EditedFileIds.Contains(file.FileId)) {
            WindowHelper.MainWindowViewModel.EditedFileIds.Add(file.FileId);
          }

          DownloadedFiles[DownloadedFiles.IndexOf(file)].IsEdited = true;
          DownloadedFiles[DownloadedFiles.IndexOf(file)].FileSize = new FileInfo(file.FilePath).Length.FormatBytes();
        }
        else {
          file.IsEdited = false;
        }
      }
    }
    catch (Exception ex) {
      Debug.WriteLine(ex.Message);
    }
  }

  [RelayCommand]
  public void OnDownloadDoubleTapped(TappedEventArgs e) {
    if (DownloadedFiles.Count > 0) {
      if (SelectedDownloadedFileIndex > -1) {
        var filePath = DownloadedFiles[SelectedDownloadedFileIndex].FilePath;
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(filePath) {
          UseShellExecute = true
        };
        process.Start();
      }
    }
  }

  public RelayCommand<Task> Process;
  public async Task ProcessCommand() => await ProcessHelper.HandleProcess(this);

  [RelayCommand]
  public async Task<bool> UploadFiles() => await new UploadService().UploadEditedFiles();
}
