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
using Newtonsoft.Json;
using urlhandler.Extensions;
using urlhandler.Helpers;
using urlhandler.Models;
using urlhandler.Services;
using urlhandler.Views;
using INotificationManager = DesktopNotifications.INotificationManager;
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
  internal readonly FileService _fileService;
  internal Process? _fileProcess;

  [ObservableProperty][NotifyPropertyChangedFor(nameof(HasFilesDownloaded))] ObservableCollection<Downloads> _downloadedFiles = [];
  [ObservableProperty] private ObservableCollection<float> _editedFileIds = [];
  [ObservableProperty] int _selectedDownloadedFileIndex = -1;
  [ObservableProperty] bool _hasFilesDownloaded;
  [ObservableProperty] private double _fileUpDownProgress;
  [ObservableProperty] string? _fileUpDownProgressText = "";
  [ObservableProperty] private string? _url;
  [ObservableProperty] string? _status = "";
  [ObservableProperty] object? _selectedUrl;
  [ObservableProperty] private bool _isAlreadyProcessing;
  [ObservableProperty] private bool _isManualEnabled;
  [ObservableProperty] string _authToken = "";
  [ObservableProperty][NotifyPropertyChangedFor(nameof(ThemeToolTip))] private bool _isDarkMode = Application.Current!.ActualThemeVariant == ThemeVariant.Dark;
  [ObservableProperty] private string _themeButtonIcon = Application.Current!.ActualThemeVariant == ThemeVariant.Dark ? "fa-solid fa-lightbulb" : "fa-regular fa-lightbulb";
  [ObservableProperty] private string _themeToolTip = Application.Current!.ActualThemeVariant == ThemeVariant.Light ? "Switch to Dark Mode" : "Switch to Light Mode";

  partial void OnIsDarkModeChanged(bool value) {
    ThemeButtonIcon = value ? "fa-solid fa-lightbulb" : "fa-regular fa-lightbulb";
    ThemeToolTip = value ? "Switch to Light Mode" : "Switch to Dark Mode";
    Application.Current!.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
  }

  public MainWindowViewModel(MainWindow mainWindow, string[] args) {
    this.mainWindow = mainWindow;
#if DEBUG
    this.args = new[] {
      "chemotion://?url=https%3A%2F%2Funcharted.chemotion.ibcs.kit.edu%2Fapi%2Fv1%2Fpublic%2Fthird_party_apps%2FeyJhbGciOiJIUzI1NiJ9.eyJhcHBJRCI6MCwidXNlcklEIjo1LCJhdHRJRCI6MTU2NTksImV4cCI6MTcxOTY5MDI1NH0.e0_3EeYYdFmL-9hO8iS4iTWT3KmgnF-2Wxu2C18LVfw"
    };
#else
    this.args = args ?? throw new ArgumentNullException(nameof(args));
#endif

    _downloadService = new DownloadService();
    _uploadService = new UploadService();
    _fileService = new FileService();
    Process = new RelayCommand<Task>(_ => Task.Run(async () => await ProcessCommand()));
    SetupEventHandlers();
  }

  private void SetupEventHandlers() {
    mainWindow.Loaded += MainWindow_Loaded;
    mainWindow.Deactivated += (s, e) => WindowHelper.Deactivate(this);
  }

  private void MainWindow_Loaded(object? sender, EventArgs e) {
    WindowHelper.Load(this);
    var timer = new Timer.Timer(1);
    timer.Elapsed += OnTimedEvent;
    timer.Enabled = true;
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

  [RelayCommand] public void OnDownloadDoubleTapped(TappedEventArgs e) => OpenFile();

  [RelayCommand]
  public void OpenFile() {
    if (DownloadedFiles.Count <= 0) return;
    if (SelectedDownloadedFileIndex <= -1) return;
    var filePath = DownloadedFiles[SelectedDownloadedFileIndex].FilePath;
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo(filePath) {
      UseShellExecute = true
    };
    process.Start();
  }

  public RelayCommand<Task> Process;
  public async Task ProcessCommand() => await ProcessHelper.HandleProcess(this, Url!);

  [RelayCommand] public async Task<bool> UploadFiles() => await new UploadService().UploadEditedFiles();

  [RelayCommand]
  public void DeleteSelectedFile() {
    if (SelectedDownloadedFileIndex > -1 &&
        (DownloadedFiles.Count > 0 && DownloadedFiles.Count > SelectedDownloadedFileIndex)) {
      if (File.Exists(DownloadedFiles[SelectedDownloadedFileIndex].FilePath))
        File.Delete(DownloadedFiles[SelectedDownloadedFileIndex].FilePath);
      DownloadedFiles.RemoveAt(SelectedDownloadedFileIndex);
      var path = AppDomain.CurrentDomain.BaseDirectory + "downloads.json";
      if (!File.Exists(path) || string.IsNullOrEmpty(File.ReadAllText(path))) return;
      var data = JsonConvert.SerializeObject(DownloadedFiles);
      File.WriteAllText(path, data);
      HasFilesDownloaded = DownloadedFiles.Count >= 1;
    }
  }

  partial void OnStatusChanged(string? oldValue, string? newValue) {
    Task.Run(async () => {
      await Task.Delay(10000);
      Status = "";
    });
  }
}
