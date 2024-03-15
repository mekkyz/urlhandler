using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using MsBox.Avalonia;
using Urlhandler.ViewModels;

namespace urlhandler.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        #region Properties
        private TrayIcon? _notifyIcon;
        private HttpClient _httpClient = new HttpClient();
        private DateTime lastWriteTimeBeforeProcessing;
        private System.Timers.Timer? _timer;
        private bool _isFileChanged;
        private string? _filePath;
        private string? _authToken;
        private string? _fileId;
        private INotificationManager? notificationManager;
        private Avalonia.Threading.DispatcherTimer? idleTimer;
        private DateTime lastInteractionTime;
        private bool isMinimizedByIdleTimer = false;
        private Notification nf = new Notification();
        #endregion Properties

        #region ObservableProperties
        [ObservableProperty]
        private double _fileUpDownProgress = 0.0f;

        [ObservableProperty]
        private string _fileUpDownProgressText = "";

        [ObservableProperty]
        private string _url = "";

        [ObservableProperty]
        private string _status = "";

        [ObservableProperty]
        private ObservableCollection<string> _history = new ObservableCollection<string>();

        [ObservableProperty]
        private bool _hasHistory = !false;

        [ObservableProperty]
        private int _selectedHistoryIndex = -1;

        [ObservableProperty]
        private object? _selectedUrl;
        private MainWindow mainWindow;

        [ObservableProperty]
        private bool _isAlreadyProcessing = false;

        [ObservableProperty]
        private bool _isManualEnabled = false;

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            #region Window Specific Events
            mainWindow.Loaded += MainWindow_Loaded;
            mainWindow.Deactivated += MainWindow_Deactivated;
            #endregion Window Specific Events

            // load History from .txt file if exists
            var historyFilePath = AppDomain.CurrentDomain.BaseDirectory + "history.txt";

            if (File.Exists(historyFilePath))
            {
                if (File.ReadAllLines(historyFilePath).Length > 0)
                {
                    foreach (var url in File.ReadAllLines(historyFilePath))
                    {
                        HasHistory = !true;
                        History.Add(url);
                    }
                }
            }
        }

        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            if (isMinimizedByIdleTimer == false && mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.ShowInTaskbar = false;
                idleTimer!.IsEnabled = false;
                idleTimer.Stop();
                isMinimizedByIdleTimer = false;
            }
            else
            {
                mainWindow.ShowInTaskbar = true;
                idleTimer!.IsEnabled = true;
                idleTimer.Start();
                isMinimizedByIdleTimer = false;
            }
        }

        private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _timer = new System.Timers.Timer(1000); // check every second for file changes
            _timer.Elapsed += async (sender, e) => await CheckFileChanges();
            InitializeSystemTrayIcon();

            if (
                Environment.OSVersion.Platform == PlatformID.Win32NT
                    && Environment.OSVersion.Version.Major >= 10
                || Environment.OSVersion.Platform == PlatformID.Unix
            )
            {
                notificationManager =
                    Program.NotificationManager
                    ?? throw new InvalidOperationException("Missing notification toast");
            }

            MinimizeWindowOnIdle();
        }
        #endregion ObservableProperties

        #region RelayCommands/Events
        partial void OnSelectedHistoryIndexChanged(int value)
        {
            if (value >= 0 && value < History.Count)
            {
                Url = History[value];
            }
        }

        [RelayCommand]
        public void AddHistory(string url)
        {
            // load History from .txt file if exists
            History.Add(url);
            var historyFilePath = AppDomain.CurrentDomain.BaseDirectory + "history.txt";
            if (File.Exists(historyFilePath))
            {
                File.AppendAllLines(historyFilePath, new List<string> { url });
            }
            else
            {
                File.WriteAllLines(historyFilePath, new List<string> { url });
            }
            HasHistory = !true;
        }

        [RelayCommand]
        public void DeleteHistory()
        {
            // load History from .txt file if exists
            var historyFilePath = AppDomain.CurrentDomain.BaseDirectory + "history.txt";
            if (SelectedHistoryIndex > -1)
            {
                History.RemoveAt(SelectedHistoryIndex);

                if (File.Exists(historyFilePath))
                {
                    File.WriteAllLines(historyFilePath, History);
                }
            }
            else
            {
                History.Clear();
                if (File.Exists(historyFilePath))
                {
                    File.Delete(historyFilePath);
                }
            }
            if (History.Count < 1)
                HasHistory = !false;
        }

        [RelayCommand]
        public async Task Process()
        {
            if (IsAlreadyProcessing == false)
            {
                if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? parsedUri))
                {
                    Status = "Invalid URL format.";
                    await ShowNotificationAsync(Status);
                    return;
                }
                var queryParams = HttpUtility.ParseQueryString(parsedUri.Query);
                _fileId = queryParams["fileId"];
                _authToken = queryParams["authtoken"];
                if (string.IsNullOrEmpty(_fileId))
                {
                    Status = "FileId is required.";
                    await ShowNotificationAsync(Status); //
                    return;
                }
                AddHistory(Url);
                _filePath = await DownloadFile(_fileId, _authToken);
                if (_filePath == null)
                {
                    Status = "Failed to download file.";
                    await ShowNotificationAsync(Status);
                    return;
                }
                IsAlreadyProcessing = true;
                ProcessFile(_filePath);
            }
            else
            {
                await MessageBoxManager
                    .GetMessageBoxStandard(
                        "Error",
                        "Another file is already being processed. Please wait for it to complete before running another."
                    )
                    .ShowAsync();
            }
        }

        #region MinimizingWindowAfterbeingIdle
        private void MinimizeWindowOnIdle()
        {
            var window = mainWindow;
            idleTimer = new Avalonia.Threading.DispatcherTimer();
            idleTimer.Interval = TimeSpan.FromSeconds(5);
            idleTimer.Tick += (sender, e) =>
            {
                var elapsedTime = DateTime.Now - lastInteractionTime;
                if (elapsedTime.TotalSeconds > 5)
                {
                    isMinimizedByIdleTimer = true;
                    mainWindow.WindowState = WindowState.Minimized;
                    idleTimer.IsEnabled = false;
                    idleTimer.Stop();
                }
            };
            idleTimer.Start();
            window.PointerPressed += Window_PointerPressed;
            window.PointerMoved += Window_PointerMoved;
            window.KeyDown += Window_KeyDown;
            lastInteractionTime = DateTime.Now;
        }

        private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            // reset interaction on key press
            lastInteractionTime = DateTime.Now;
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            // reset interaction on mouse move
            lastInteractionTime = DateTime.Now;
        }

        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // reset interaction on mouse click
            lastInteractionTime = DateTime.Now;
        }

        private void OnActivated(object sender, EventArgs e)
        {
            // reset interaction on window focus
            lastInteractionTime = DateTime.Now;
        }
        #endregion MinimizingWindowAfterbeingIdle

        #region Uploading/Downloading
        private async Task<string?> DownloadFile(string? fileId, string? authtoken)
        {
            if (fileId == null)
                return null;
            string downloadUrl =
                $"http://127.0.0.1:3000/download?fileId={fileId}&authtoken={authtoken}";
            try
            {
                Status = "Downloading File...";
                await ShowNotificationAsync(Status);
                var progress = new Progress<ProgressInfo>(progress =>
                {
                    FileUpDownProgressText =
                        $"Downloaded {(progress.BytesRead / 1024) / 1024} MBs out of {(progress.TotalBytesExpected / 1024) / 1024} MBs.";

                    Status = FileUpDownProgressText;

                    FileUpDownProgress = progress.Percentage;
                });
                var (response, fileContentBytes) = await _httpClient.GetWithProgressAsync(
                    downloadUrl,
                    progress
                );
                Status = response.IsSuccessStatusCode
                    ? "File downloaded successfully."
                    : "Failed to download file.";
                await ShowNotificationAsync(Status);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                string filePath = Path.Combine(Path.GetTempPath(), fileId);
                using (
                    FileStream fileStream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write
                    )
                )
                {
                    await fileStream.WriteAsync(fileContentBytes, 0, fileContentBytes.Length);
                }
                return filePath;
            }
            catch
            {
                return null;
            }
        }

        private string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "KB", "MB", "GB", "TB" };
            long max = (long)Math.Pow(scale, orders.Length - 1);
            for (int i = 0; i < orders.Length; i++)
            {
                long scaleMax = (long)Math.Pow(scale, i + 1);
                if (bytes < scaleMax)
                {
                    return String.Format(
                        "{0:##.##} {1}",
                        decimal.Divide(bytes, scaleMax / scale),
                        orders[i]
                    );
                }
            }
            return String.Format("{0:##.##} TB", decimal.Divide(bytes, (long)Math.Pow(scale, 3)));
        }

        private async Task<bool> UploadFile(string? filePath, string? authToken)
        {
            if (filePath == null || authToken == null)
                return false;
            string uploadUrl = $"http://127.0.0.1:3000/upload?authtoken={authToken}";
            try
            {
                byte[] fileContentBytes = File.ReadAllBytes(filePath);
                var content = new MultipartFormDataContent();
                content.Add(
                    new ByteArrayContent(fileContentBytes),
                    name: "file",
                    new FileInfo(filePath).Name
                );
                var progress = new Progress<ProgressInfo>(progress =>
                {
                    FileUpDownProgressText =
                        $"Uploaded {(progress.BytesRead / 1024) / 1024} MBs out of {(progress.TotalBytesExpected / 1024) / 1024} MBs.";
                    Status = FileUpDownProgressText;
                    FileUpDownProgress = progress.Percentage;
                });
                var response = await _httpClient.PostWithProgressAsync(
                    uploadUrl,
                    content,
                    progress
                );
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        #endregion Uploading/Downloading

        #region File Processing
        private Process? _fileProcess;

        private void ProcessFile(string? filePath)
        {
            try
            {
                if (filePath != null)
                {
                    _fileProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true }
                    };
                    _fileProcess.EnableRaisingEvents = true;
                    _fileProcess.Exited += (sender, args) =>
                    {
                        IsAlreadyProcessing = false;
                    };
                    _fileProcess.Start();
                    _timer.Start();
                    lastWriteTimeBeforeProcessing = File.GetLastWriteTime(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
        }

        private async Task CheckFileChanges()
        {
            if (_filePath == null || _fileId == null || _authToken == null)
                return;
            try
            {
                DateTime lastWriteTimeAfterProcessing = File.GetLastWriteTime(_filePath);
                if (lastWriteTimeBeforeProcessing != lastWriteTimeAfterProcessing)
                {
                    _isFileChanged = true;
                    _timer.Stop();
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        Status = "File has been modified, uploading...";
                        await ShowNotificationAsync(Status);

                        // continue with file upload
                        bool uploadSuccess = await UploadFile(_filePath, _authToken);

                        Status = uploadSuccess
                            ? "File uploaded successfully."
                            : "Failed to upload file.";
                        await ShowNotificationAsync(Status);
                        IsManualEnabled = true;
                        return;
                    });
                }
                else
                {
                    TimeSpan elapsedTime = DateTime.Now - lastWriteTimeBeforeProcessing;
                    if (elapsedTime.TotalMinutes >= 5) // timeout duration
                    {
                        IsManualEnabled = true;
                    }
                }
            }
            catch (IOException ex)
            {
                // if file in use
                Console.WriteLine($"File access error: {ex.Message}");
            }
        }

        partial void OnIsManualEnabledChanged(bool value)
        {
            if (value == false)
            {
                IsAlreadyProcessing = false;
            }
            else
            {
                IsAlreadyProcessing = true;
            }
        }

        #endregion File Processing

        private void InitializeSystemTrayIcon()
        {
            var _trayMenu = new NativeMenu();
            _trayMenu.Add(
                new NativeMenuItem
                {
                    Header = "Finish Editing and Upload File Now",
                    Command = new RelayCommand(async () =>
                    {
                        if (_isFileChanged && _filePath != null && _authToken != null)
                        {
                            Status = "Uploading file...";
                            await ShowNotificationAsync(Status);
                            bool uploadSuccess = await UploadFile(_filePath, _authToken);
                            Status = uploadSuccess
                                ? "File uploaded successfully."
                                : "Failed to upload file.";
                            await ShowNotificationAsync(Status);
                            IsManualEnabled = true;
                        }
                    })
                }
            );

            _trayMenu.Add(
                new NativeMenuItem
                {
                    Header = "Exit",
                    Command = new RelayCommand(() =>
                    {
                        if (
                            Application.Current?.ApplicationLifetime
                            is IClassicDesktopStyleApplicationLifetime desktopApp
                        )
                        {
                            desktopApp.Shutdown();
                        }
                    })
                }
            );

            _notifyIcon = new TrayIcon
            {
                Icon = new("icon.ico"),
                IsVisible = true,
                ToolTipText = "Url Handler",
                Menu = _trayMenu
            };
            // wire up events
            _notifyIcon.Clicked += (sender, e) => ShowMainWindow();
        }

        private async Task ShowNotificationAsync(string body, string title = "Status")
        {
            if (
                Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktopApp
            )
            {
                if (desktopApp.MainWindow!.WindowState == WindowState.Minimized)
                {
                    if (
                        Environment.OSVersion.Platform == PlatformID.Win32NT
                            && Environment.OSVersion.Version.Major >= 10
                        || Environment.OSVersion.Platform == PlatformID.Unix
                    )
                    {
                        nf = new Notification
                        {
                            Title = title,
                            Body = body,
                            BodyImagePath = AppDomain.CurrentDomain.BaseDirectory + "icon.ico"
                        };
                        await notificationManager!.ShowNotification(nf);
                    }
                }
            }
        }

        private void ShowMainWindow()
        {
            if (
                Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktopApp
            )
            {
                desktopApp.MainWindow!.Show();
                desktopApp.MainWindow!.WindowState = WindowState.Normal;
                desktopApp.MainWindow!.ShowInTaskbar = true;
            }
        }
        #endregion RelayCommands/Events
    }
}
