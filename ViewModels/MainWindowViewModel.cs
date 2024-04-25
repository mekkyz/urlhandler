using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using MsBox.Avalonia;
using urlhandler.Model;
using Urlhandler.ViewModels;

namespace urlhandler.ViewModels {
  public partial class MainWindowViewModel : ViewModelBase {
    private TrayIcon? _notifyIcon;
    private HttpClient _httpClient = new HttpClient();
    private string? _filePath;
    private string? _fileId;
    private INotificationManager? notificationManager;
    private DispatcherTimer? idleTimer;
    private DateTime lastInteractionTime;
    private bool isMinimizedByIdleTimer = false;
    private MainWindow mainWindow;
    private string[] args = ["", ""];

    [ObservableProperty][NotifyPropertyChangedFor(nameof(HasFilesDownloaded))] private ObservableCollection<Downloads> _downloadedFiles = new ObservableCollection<Downloads>();
    [ObservableProperty] private int _selectedDownloadedFileIndex = -1;
    [ObservableProperty] private bool _hasFilesDownloaded = !false;
    [ObservableProperty] private double _fileUpDownProgress = 0.0f;
    [ObservableProperty] private string _fileUpDownProgressText = "";
    [ObservableProperty] private string _url = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private ObservableCollection<string> _history = new ObservableCollection<string>();
    [ObservableProperty] private bool _hasHistory = !false;
    [ObservableProperty] private int _selectedHistoryIndex = -1;
    [ObservableProperty] private object? _selectedUrl;
    [ObservableProperty] private bool _isAlreadyProcessing = false;
    [ObservableProperty] private bool _isManualEnabled = false;
    [ObservableProperty] private int? _authToken;

    public MainWindowViewModel(MainWindow mainWindow, string[] _args) {
      this.mainWindow = mainWindow;
      args = _args ?? throw new ArgumentNullException(nameof(_args));

      mainWindow.Loaded += MainWindow_Loaded;
      mainWindow.Deactivated += MainWindow_Deactivated;

      // load History from .txt file if exists
      var historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.txt");

      if (File.Exists(historyFilePath)) {
        var lines = File.ReadAllLines(historyFilePath);
        if (lines.Length > 0) {
          foreach (var url in lines) {
            HasHistory = true;
            History.Add(url);
          }
        }
      }
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e) {
      if (isMinimizedByIdleTimer == false && mainWindow.WindowState == WindowState.Minimized) {
        mainWindow.ShowInTaskbar = false;
        if (idleTimer != null) {
          idleTimer.IsEnabled = false;
          idleTimer.Stop();
        }
        isMinimizedByIdleTimer = true;
      }
      else {
        mainWindow.ShowInTaskbar = true;
        if (idleTimer != null) {
          idleTimer.IsEnabled = true;
          idleTimer.Start();
        }
        isMinimizedByIdleTimer = false;
      }
    }

    private void MainWindow_Loaded(object? sender, EventArgs e) {
      InitializeSystemTrayIcon();

      if ((Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10) ||
          Environment.OSVersion.Platform == PlatformID.Unix) {
        notificationManager = Program.NotificationManager ?? throw new InvalidOperationException("Missing notification manager");
      }

      Task.Run(async () => {
        await FetchAuthToken();
        if (args.Length > 0) {
          string pattern = @"^chemotion://\d+/(\w+\.\w+)/(\d+)$";
          Regex regex = new Regex(pattern);
          Match match = regex.Match(args.First());

          if (!match.Success) {
            Status = "Invalid URL format. Expected format: chemotion://3000/csii.png/12345";
            await ShowNotificationAsync(Status);
            return;
          }

          string fileName = match.Groups[1].Value;
          string authToken = match.Groups[2].Value;

          string downloadUrl = $"http://localhost:3000/download?fileId={fileName}&authtoken={authToken}";

          _filePath = await DownloadFile(fileName, int.Parse(authToken));
          if (_filePath == null) {
            Status = "Failed to download file.";
            await ShowNotificationAsync(Status);
            return;
          }

          AddHistory(downloadUrl);
          ProcessFile(_filePath);
        }
      });

      MinimizeWindowOnIdle();
    }

    private async Task FetchAuthToken() {
      try {
        var response = await _httpClient.GetAsync("http://localhost:3000/get_token");
        if (response.IsSuccessStatusCode) {
          var content = await response.Content.ReadAsStringAsync();
          if (int.TryParse(content, out int token)) {
            AuthToken = token;
          }
          else {
            // handle invalid token format
            throw new InvalidOperationException("Failed to parse auth token from response content.");
          }
        }
        else {
          // handle unsuccessful HTTP response
          throw new HttpRequestException($"Failed to fetch auth token. Status code: {response.StatusCode}");
        }
      }
      catch (HttpRequestException ex) {
        Console.WriteLine($"Error fetching auth token: {ex.Message}");
        throw;
      }
      catch (Exception ex) {
        Console.WriteLine($"Error fetching auth token: {ex.Message}");
        throw;
      }
    }

    partial void OnSelectedHistoryIndexChanged(int value) {
      try {
        if (value >= 0 && value < History.Count) {
          Url = History[value];
        }
        else {
          throw new IndexOutOfRangeException("Selected history index is out of range.");
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error in OnSelectedHistoryIndexChanged: {ex.Message}");
        throw;
      }
    }

    [RelayCommand]
    public void AddHistory(string url) {
      try {
        // load History from .txt file if exists
        History.Add(url);
        var historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.txt");
        if (File.Exists(historyFilePath)) {
          File.AppendAllLines(historyFilePath, new[] { url });
        }
        else {
          File.WriteAllLines(historyFilePath, new[] { url });
        }

        HasHistory = History.Any();
      }
      catch (Exception ex) {
        Console.WriteLine($"Error in AddHistory: {ex.Message}");
        throw;
      }
    }

    [RelayCommand]
    public void DeleteHistory() {
      try {
        var historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.txt");

        if (SelectedHistoryIndex > -1 && SelectedHistoryIndex < History.Count) {
          History.RemoveAt(SelectedHistoryIndex);
        }
        else {
          History.Clear();
        }
        if (File.Exists(historyFilePath)) {
          if (History.Count > 0) {
            File.WriteAllLines(historyFilePath, History);
          }
          else {
            File.Delete(historyFilePath);
          }
        }

        HasHistory = History.Any();
      }
      catch (Exception ex) {
        // Handle exceptions
        Console.WriteLine($"Error in DeleteHistory: {ex.Message}");
        throw;
      }
    }

    [RelayCommand]
    public async Task Process() {
      try {
        if (IsAlreadyProcessing == false) {
          if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? parsedUri)) {
            Status = "Invalid URL format.";
            await ShowNotificationAsync(Status);
            return;
          }

          var queryParams = HttpUtility.ParseQueryString(parsedUri.Query);
          _fileId = queryParams["fileId"];
          var authTokenString = queryParams["authtoken"];
          if (string.IsNullOrEmpty(_fileId)) {
            Status = "FileId is required.";
            await ShowNotificationAsync(Status);
            return;
          }
          if (string.IsNullOrEmpty(authTokenString) || !int.TryParse(authTokenString, out var _authToken)) {
            Status = "Auth token is invalid or missing.";
            await ShowNotificationAsync(Status);
            return;
          }

          AddHistory(Url);
          _filePath = await DownloadFile(_fileId, _authToken);
          if (_filePath == null) {
            await FetchAuthToken();
            if (_authToken != AuthToken) {
              Status = "Failed to download with old token. Please use a fresh token.";
              await ShowNotificationAsync(Status);
              return;
            }
            Status = "Failed to download file.";
            await ShowNotificationAsync(Status);
            return;
          }

          ProcessFile(_filePath);
        }
        else {
          await MessageBoxManager
              .GetMessageBoxStandard(
                  "Error",
                  "Another file is already being processed. Please wait for it to complete before running another."
              )
              .ShowAsync();
        }
      }
      catch (HttpRequestException ex) {
        string detailedError = $"Network error occurred: {ex.Message}. Please check your connection or contact support if the problem persists.";
        await MessageBoxManager.GetMessageBoxStandard("Error", detailedError).ShowAsync();
      }
      catch (Exception ex) {
        Console.WriteLine($"Error in Process method: {ex.Message}");
        throw;
      }
    }

    private void MinimizeWindowOnIdle() {
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
            await ShowNotificationAsync("The window has been minimized due to inactivity. Move the mouse or press any key to restore.", "Auto-minimize");
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

    private void ResetLastInteractionTime() {
      // reset last interaction time
      lastInteractionTime = DateTime.Now;
      if (isMinimizedByIdleTimer) {
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.ShowInTaskbar = true;
        isMinimizedByIdleTimer = false;
        if (idleTimer != null) {
          idleTimer.IsEnabled = true;
          idleTimer.Start();
        }
        else {
          Console.WriteLine("Error: idleTimer is null.");
        }
        ShowNotificationAsync("The window has been restored after being idle. You can continue your work.", "Window Restored").Wait();
      }
    }


    public async Task DownloadFilesConcurrently(IEnumerable<string> fileIds, int authtoken) {
      var downloadTasks = fileIds.Select(fileId => DownloadFile(fileId, authtoken));
      var files = await Task.WhenAll(downloadTasks);
      foreach (var file in files.Where(f => f != null)) {
        ProcessFile(file);
      }
    }

    private async Task<string?> DownloadFile(string? fileId, int authtoken) {
      try {
        if (fileId == null) {
          Status = "FileId is required.";
          await ShowNotificationAsync(Status);
          return null;
        }
        if (authtoken != AuthToken) {
          Status = "Failed to download with old token. Please use a fresh token.";
          await ShowNotificationAsync(Status);
          return null;
        }
        string downloadUrl = $"http://127.0.0.1:3000/download?fileId={fileId}&authtoken={authtoken}";
        Status = "Downloading File...";
        await ShowNotificationAsync(Status);
        var progress = new Progress<ProgressInfo>(progressInfo => {
          Dispatcher.UIThread.Invoke(() => {
            FileUpDownProgressText = $"Downloaded {FormatBytes(progressInfo.BytesRead)} out of {FormatBytes(progressInfo.TotalBytesExpected ?? 0)}.";
            FileUpDownProgress = progressInfo.Percentage;
            Status = FileUpDownProgressText;
          });
        });
        var (response, fileContentBytes) = await _httpClient.GetWithProgressAsync(downloadUrl, progress);
        if (!response.IsSuccessStatusCode) {
          Status = "Failed to download file.";
          await ShowNotificationAsync(Status);
          return null;
        }

        string filePath = Path.Combine(Path.GetTempPath(), fileId);
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) {
          await fileStream.WriteAsync(fileContentBytes.AsMemory(0, fileContentBytes.Length));
        }

        await FetchAuthToken();

        Status = "File downloaded successfully.";
        await ShowNotificationAsync(Status);
        return filePath;
      }
      catch (Exception ex) {
        string errorMessage = ex is IOException ?
            $"File access error: {ex.Message} - The file might be in use by another process or locked." :
            $"An error occurred: {ex.Message}";

        Status = errorMessage;
        await ShowNotificationAsync(Status);

        return null;
      }
    }

    private Timer? _debounceTimer;
    private void DebounceUpdate(Action updateAction, int interval = 300) {
      _debounceTimer?.Dispose();
      _debounceTimer = new Timer(_ => {
        updateAction();
        _debounceTimer = null;
      }, null, interval, Timeout.Infinite);
    }

    private void UpdateProgress(ProgressInfo progress, string fileId) {
      DebounceUpdate(() => {
        Dispatcher.UIThread.Invoke(() => {
          FileUpDownProgressText = $"Downloaded {FormatBytes(progress.BytesRead)} out of {FormatBytes(progress.TotalBytesExpected ?? 0)} for file {fileId}.";
          FileUpDownProgress = progress.Percentage;
        });
      }, 300);
    }

    private string FormatBytes(long bytes) {
      const int scale = 1024;
      string[] orders = new string[] { "B", "KB", "MB", "GB", "TB" };
      int orderIndex = 0;
      decimal adjustedBytes = bytes;
      while (adjustedBytes >= scale && orderIndex < orders.Length - 1) {
        adjustedBytes /= scale;
        orderIndex++;
      }
      // format the bytes with the appropriate unit
      return $"{adjustedBytes:##.##} {orders[orderIndex]}";
    }

    public async Task<bool> UploadFiles(bool ignoreIndex = false) {
      bool allUploadsSuccessful = true;

      try {
        // get list of uploaded files
        List<Downloads> previouslyUploadedFiles = DownloadedFiles.ToList();

        if (DownloadedFiles.Count > 0 && ignoreIndex) {
          // upload all files that were modified
          for (int i = DownloadedFiles.Count - 1; i >= 0; i--) {
            string filePath = DownloadedFiles[i].FilePath;

            if (filePath == null || AuthToken == null)
              return false;

            string uploadUrl = $"http://127.0.0.1:3000/upload?authtoken={AuthToken}";

            // check if file modified since last upload
            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
            Downloads previouslyUploadedFile =
              previouslyUploadedFiles.Find(f => f.FilePath == filePath)
              ?? throw new InvalidOperationException("File not found.");
            if (previouslyUploadedFile != null && lastWriteTime <= previouslyUploadedFile.FileTime) {
              Status = $"File {i + 1} ({new FileInfo(filePath).Name}) has not been modified since the last upload. Skipping...";
              await ShowNotificationAsync(Status);
              continue;
            }

            byte[] fileContentBytes = await File.ReadAllBytesAsync(filePath);
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileContentBytes), "file", Path.GetFileName(filePath));
            var progress = new Progress<ProgressInfo>(progress => {
              FileUpDownProgressText = $"Uploaded {FormatBytes(progress.BytesRead)} out of {FormatBytes(progress.TotalBytesExpected ?? 0)}.";
              Status = FileUpDownProgressText;
              FileUpDownProgress = progress.Percentage;
            });
            var response = await _httpClient.PostWithProgressAsync(uploadUrl, content, progress);
            Status = response.IsSuccessStatusCode ? "File uploaded successfully." : "Failed to upload file.";
            await ShowNotificationAsync(Status);
            DownloadedFiles.RemoveAt(i);
          }
        }
      }
      catch (Exception ex) {
        Status = $"Error uploading files: {ex.Message}";
        await ShowNotificationAsync(Status);
        allUploadsSuccessful = false;
      }
      return allUploadsSuccessful;
    }

    [RelayCommand]
    public async Task<bool> UploadFiles() {
      bool allUploadsSuccessful = true;

      try {
        if (DownloadedFiles == null || DownloadedFiles.Count < 1) {
          return false;
        }

        List<Downloads> previouslyUploadedFiles = DownloadedFiles.ToList();

        // upload all files that have been modified
        for (int i = DownloadedFiles.Count - 1; i >= 0; i--) {
          string filePath = DownloadedFiles[i].FilePath;

          if (filePath == null || AuthToken == null)
            return false;

          string uploadUrl = $"http://127.0.0.1:3000/upload?authtoken={AuthToken}";

          try {
            // check if file modified
            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
            Downloads? previouslyUploadedFile = previouslyUploadedFiles?.Find(f => f.FilePath == filePath);
            if (previouslyUploadedFile != null && lastWriteTime <= previouslyUploadedFile.FileTime) {
              Status = $"File {i + 1} ({new FileInfo(filePath).Name}) has not been modified since the last upload. Skipping...";
              await ShowNotificationAsync(Status);
              continue;
            }

            byte[] fileContentBytes = await File.ReadAllBytesAsync(filePath);
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileContentBytes), "file", Path.GetFileName(filePath));
            var progress = new Progress<ProgressInfo>(progress => {
              FileUpDownProgressText = $"Uploaded {FormatBytes(progress.BytesRead)} out of {FormatBytes(progress.TotalBytesExpected ?? 0)}.";
              Status = FileUpDownProgressText;
              FileUpDownProgress = progress.Percentage;
            });
            var response = await _httpClient.PostWithProgressAsync(uploadUrl, content, progress);
            Status = response.IsSuccessStatusCode ? "File uploaded successfully." : "Failed to upload file.";
            await ShowNotificationAsync(Status);
            DownloadedFiles.RemoveAt(i);
          }
          catch (Exception ex) {
            Status = $"Error uploading File {i + 1}: {ex.Message}";
            await ShowNotificationAsync(Status);
            allUploadsSuccessful = false;
          }
        }
        if (DownloadedFiles.Count < 1) {
          HasFilesDownloaded = false;
        }
        else {
          HasFilesDownloaded = true;
        }
      }
      catch (Exception ex) {
        Status = $"Error uploading files: {ex.Message}";
        await ShowNotificationAsync(Status);
        allUploadsSuccessful = false;
      }
      return allUploadsSuccessful;
    }

    private Process? _fileProcess;

    private void ProcessFile(string? filePath) {
      try {
        if (filePath != null) {
          _fileProcess?.Dispose();

          _fileProcess = new Process {
            StartInfo = new ProcessStartInfo(filePath) {
              UseShellExecute = true
            }
          };
          _fileProcess.Start();

          DownloadedFiles.Add(new Downloads() {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            FileTime = File.GetLastWriteTime(filePath)
          });
          HasFilesDownloaded = DownloadedFiles.Count > 0;
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error processing file: {ex.Message}");
      }
    }

    private void InitializeSystemTrayIcon() {
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
              await UploadFiles(ignoreIndex: true);
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

    private async Task ShowNotificationAsync(string body, string title = "Status") {
      try {
        string detailedBody = body;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10
            || Environment.OSVersion.Platform == PlatformID.Unix) {
          if (body.StartsWith("Failed to download")) {
            detailedBody += " Please check your network connection and ensure the file ID and authentication token are correct.";
          }
          else if (body.StartsWith("Invalid URL format")) {
            detailedBody += " Ensure the URL matches the expected pattern.";
          }
          else if (body.StartsWith("File access error")) {
            detailedBody += " Make sure the file is not being used by another application and you have sufficient permissions.";
          }
          Notification nf = new Notification {
            Title = title,
            Body = detailedBody,
            BodyImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico")
          };
          await notificationManager!.ShowNotification(nf);
        }
      }
      catch (Exception ex) {
        Debug.WriteLine($"Error showing notification: {ex.Message}");
      }
    }

    private void ShowMainWindow() {
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
