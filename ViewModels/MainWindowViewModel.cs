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

namespace urlhandler.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        #region Properties

        private TrayIcon? _notifyIcon;
        private HttpClient _httpClient = new HttpClient();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFilesDownloaded))]
        private ObservableCollection<Downloads> _downloadedFiles = new ObservableCollection<Downloads>();
        [ObservableProperty] private int _selectedDownloadedFileIndex = -1;
        [ObservableProperty] private bool _hasFilesDownloaded = !false;


        //private System.Timers.Timer? _timer;
        private bool _isFileChanged;
        private string? _filePath;
        [ObservableProperty] private int _authToken;
        private string? _fileId;
        private INotificationManager? notificationManager;
        private Avalonia.Threading.DispatcherTimer? idleTimer;
        private DateTime lastInteractionTime;
        private bool isMinimizedByIdleTimer = false;
        private Notification nf = new Notification();

        #endregion Properties

        #region ObservableProperties

        [ObservableProperty] private double _fileUpDownProgress = 0.0f;

        [ObservableProperty] private string _fileUpDownProgressText = "";

        [ObservableProperty] private string _url = "";

        [ObservableProperty] private string _status = "";

        [ObservableProperty] private ObservableCollection<string> _history = new ObservableCollection<string>();


        [ObservableProperty] private bool _hasHistory = !false;

        [ObservableProperty] private int _selectedHistoryIndex = -1;

        [ObservableProperty] private object? _selectedUrl;
        private MainWindow mainWindow;

        [ObservableProperty] private bool _isAlreadyProcessing = false;

        [ObservableProperty] private bool _isManualEnabled = false;

        string[] args = ["", ""];
        public MainWindowViewModel(MainWindow mainWindow, string[] _args)
        {
            this.mainWindow = mainWindow;
            args = _args;
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
            // _timer = new System.Timers.Timer(1000); 
            // _timer.Elapsed += async (sender, e) => await CheckFileChanges();

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

            Task.Run(async () =>
            {
                await FetchAuthToken();
                if (args.Length > 0)
                {
                    string pattern = @"^localhost://\d+/(\w+\.\w+)/(\d+)$";
                    Regex regex = new Regex(pattern);
                    Match match = regex.Match(args.First());

                    if (!match.Success)
                    {
                        Status = "Invalid URL format. Expected format: localhost://3000/csii.png/12345";
                        await ShowNotificationAsync(Status);
                        return;
                    }

                    string fileName = match.Groups[1].Value;
                    string authToken = match.Groups[2].Value;

                    string downloadUrl = $"http://localhost:3000/download?fileId={fileName}&authtoken={authToken}";

                    _filePath = await DownloadFile(fileName, int.Parse(authToken));
                    if (_filePath == null)
                    {
                        Status = "Failed to download file.";
                        await ShowNotificationAsync(Status);
                        return;
                    }

                    AddHistory(downloadUrl);
                    //IsAlreadyProcessing = true;
                    ProcessFile(_filePath);
                }

            });


            MinimizeWindowOnIdle();
        }

        private async Task FetchAuthToken()
        {
            var response = await _httpClient.GetAsync("http://localhost:3000/get_token");
            if (response.IsSuccessStatusCode)
            {
                var c = await response.Content.ReadAsStringAsync();
                AuthToken = int.Parse(c);
            }
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
            try
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
                    var _authToken = int.Parse(queryParams["authtoken"]);
                    if (string.IsNullOrEmpty(_fileId))
                    {
                        Status = "FileId is required.";
                        await ShowNotificationAsync(Status); //
                        return;
                    }

                    AddHistory(Url);
                    _filePath = await DownloadFile(_fileId, _authToken);
                    if (AuthToken <= 0)
                    {
                        await FetchAuthToken();
                        if (_filePath == null && _authToken != AuthToken)
                        {
                            Status = "Failed to download with old token. Please use a fresh token.";
                            await ShowNotificationAsync(Status);
                            return;
                        }
                        if (_filePath == null)
                        {
                            Status = "Failed to download file.";
                            await ShowNotificationAsync(Status);
                            return;
                        }
                    }
                    if (_filePath == null && _authToken != AuthToken)
                    {
                        Status = "Failed to download with old token. Please use a fresh token.";
                        await ShowNotificationAsync(Status);
                        return;
                    }

                    if (_filePath == null)
                    {
                        Status = "Failed to download file.";
                        await ShowNotificationAsync(Status);
                        return;
                    }

                    //IsAlreadyProcessing = true;
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
            catch (HttpRequestException ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard(
                        "Error",
                        "Error 400: Either no internet connection or server is not working."
                    )
                    .ShowAsync();
            }
        }

        #region MinimizingWindowAfterbeingIdle

        private void MinimizeWindowOnIdle()
        {
            var window = mainWindow;
            idleTimer = new Avalonia.Threading.DispatcherTimer();
            idleTimer.Interval = TimeSpan.FromSeconds(300);
            idleTimer.Tick += (sender, e) =>
            {
                var elapsedTime = DateTime.Now - lastInteractionTime;
                if (elapsedTime.TotalSeconds > 300)
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

        private async Task<string?> DownloadFile(string? fileId, int authtoken)
        {

            if (fileId == null)
                return null;
            if (authtoken != AuthToken)
            {
                Status = "Failed to download with old token. Please use a fresh token.";
                await ShowNotificationAsync(Status);
                return null;
            }
            string downloadUrl =
                $"http://127.0.0.1:3000/download?fileId={fileId}&authtoken={authtoken}";
            try
            {
                Status = "Downloading File...";
                await ShowNotificationAsync(Status);
                var progress = new Progress<ProgressInfo>(progress =>
                {
                    FileUpDownProgressText =
                        $"Downloaded {FormatBytes(progress.BytesRead)} out of {FormatBytes(progress.TotalBytesExpected ?? 0)}.";

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

                await FetchAuthToken();
                string filePath = Path.Combine(Path.GetTempPath(), fileId);
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.WriteAsync(fileContentBytes, 0, fileContentBytes.Length);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Status = "System.IO.IOException: The process cannot access the file because it is being used by another process";
                await ShowNotificationAsync(Status);
                return null;
            }
        }

        private string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "B", "KB", "MB", "GB", "TB" };
            long max = (long)Math.Pow(scale, orders.Length - 1);
            for (int i = 0; i < orders.Length; i++)
            {
                long scaleMax = (long)Math.Pow(scale, i + 1);
                if (bytes < scale)
                {
                    return $"{bytes} {orders[0]}"; // special case for bytes
                }
                if (bytes < scaleMax)
                {
                    return String.Format(
                        "{0:##.##} {1}",
                        decimal.Divide(bytes, scaleMax / scale),
                        orders[i]
                    );
                }
            }

            return String.Format("{0:##.##} TB", decimal.Divide(bytes, (long)Math.Pow(scale, orders.Length)));
        }


        [RelayCommand]
        public async Task<bool> UploadFiles(bool ignoreIndex = false)
        {
            bool allUploadsSuccessful = true;

            // get list of uploaded files
            List<Downloads> previouslyUploadedFiles = DownloadedFiles.ToList();

            if (DownloadedFiles.Count > 0 && ignoreIndex == true)
            {
                // upload all files that were modified
                for (int i = DownloadedFiles.Count - 1; i >= 0; i--)
                {
                    string filePath = DownloadedFiles[i].FilePath;

                    if (filePath == null || AuthToken == null)
                        return false;

                    string uploadUrl = $"http://127.0.0.1:3000/upload?authtoken={AuthToken}";

                    try
                    {
                        // check if file modified since last upload
                        DateTime lastWriteTime = File.GetLastWriteTime(filePath);
                        Downloads previouslyUploadedFile = previouslyUploadedFiles.Find(f => f.FilePath == filePath);
                        if (previouslyUploadedFile != null && lastWriteTime <= previouslyUploadedFile.FileTime)
                        {
                            Status =
                                $"File {i + 1} ({new FileInfo(filePath).Name}) has not been modified since the last upload. Skipping...";
                            await ShowNotificationAsync(Status);
                            continue;
                        }

                        byte[] fileContentBytes =
                            await File.ReadAllBytesAsync(DownloadedFiles[i].FilePath);
                        var content = new MultipartFormDataContent();
                        content.Add(
                            new ByteArrayContent(fileContentBytes),
                            name: "file",
                            new FileInfo(DownloadedFiles[i].FilePath).Name
                        );
                        var progress = new Progress<ProgressInfo>(progress =>
                        {
                            FileUpDownProgressText =
                                $"Uploaded {FormatBytes(progress.BytesRead)} out of {FormatBytes(progress.TotalBytesExpected ?? 0)}.";
                            Status = FileUpDownProgressText;
                            FileUpDownProgress = progress.Percentage;
                        });
                        var response = await _httpClient.PostWithProgressAsync(
                            uploadUrl,
                            content,
                            progress
                        );
                        Status = response.IsSuccessStatusCode

                            ? $"File uploaded successfully."
                            : $"Failed to upload File.";
                        await ShowNotificationAsync(Status);
                        DownloadedFiles.RemoveAt(i);
                        return true;

                    }
                    catch (Exception ex)
                    {
                        Status = $"Error uploading File {i + 1}: {ex.Message}";
                        await ShowNotificationAsync(Status);
                        allUploadsSuccessful = false;
                    }
                }

                if (SelectedDownloadedFileIndex == null || DownloadedFiles.Count > 1)
                {
                    // upload all files that have been modified
                    for (int i = DownloadedFiles.Count - 1; i >= 0; i--)
                    {
                        string filePath = DownloadedFiles[i].FilePath;

                        if (filePath == null || AuthToken == null)
                            return false;

                        string uploadUrl = $"http://127.0.0.1:3000/upload?authtoken={AuthToken}";

                        try
                        {
                            // check if the file has been modified since the last upload
                            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
                            Downloads previouslyUploadedFile =
                                previouslyUploadedFiles.Find(f => f.FilePath == filePath);
                            if (previouslyUploadedFile != null && lastWriteTime <= previouslyUploadedFile.FileTime)
                            {
                                Status =
                                    $"File {i + 1} ({new FileInfo(filePath).Name}) has not been modified since the last upload. Skipping...";
                                await ShowNotificationAsync(Status);
                                continue;
                            }

                            byte[] fileContentBytes =
                                await File.ReadAllBytesAsync(DownloadedFiles[i].FilePath);
                            var content = new MultipartFormDataContent();
                            content.Add(
                                new ByteArrayContent(fileContentBytes),
                                name: "file",
                                new FileInfo(DownloadedFiles[i].FilePath).Name
                            );
                            var progress = new Progress<ProgressInfo>(progress =>
                            {
                                FileUpDownProgressText =
                                    $"Uploaded {FormatBytes(progress.BytesRead)} out of {FormatBytes(progress.TotalBytesExpected ?? 0)}.";
                                Status = FileUpDownProgressText;
                                FileUpDownProgress = progress.Percentage;
                            });
                            var response = await _httpClient.PostWithProgressAsync(
                                uploadUrl,
                                content,
                                progress
                            );
                            Status = response.IsSuccessStatusCode

                                ? $"File uploaded successfully."
                                : $"Failed to upload File.";
                            await ShowNotificationAsync(Status);
                            DownloadedFiles.RemoveAt(i);
                            return true;

                        }
                        catch (Exception ex)
                        {
                            Status = $"Error uploading File {i + 1}: {ex.Message}";
                            await ShowNotificationAsync(Status);
                            allUploadsSuccessful = false;
                        }
                    }
                }
                else
                {
                    // upload selected file if modified
                    int selectedIndex = SelectedDownloadedFileIndex;
                    string filePath = DownloadedFiles[selectedIndex].FilePath;

                    if (filePath == null || AuthToken == null)
                        return false;

                    string uploadUrl = $"http://127.0.0.1:3000/upload?authtoken={AuthToken}";

                    try
                    {
                        DateTime lastWriteTime = File.GetLastWriteTime(filePath);
                        Downloads previouslyUploadedFile = previouslyUploadedFiles.Find(f => f.FilePath == filePath);
                        if (previouslyUploadedFile != null && lastWriteTime <= previouslyUploadedFile.FileTime)
                        {
                            Status =
                                $"File ({new FileInfo(filePath).Name}) has not been modified since the last upload. Skipping...";
                            await ShowNotificationAsync(Status);
                        }
                        else
                        {
                            byte[] fileContentBytes =
                                await File.ReadAllBytesAsync(DownloadedFiles[SelectedDownloadedFileIndex].FilePath);
                            var content = new MultipartFormDataContent();
                            content.Add(
                                new ByteArrayContent(fileContentBytes),
                                name: "file",
                                new FileInfo(DownloadedFiles[SelectedDownloadedFileIndex].FilePath).Name
                            );
                            var progress = new Progress<ProgressInfo>(progress =>
                            {
                                FileUpDownProgressText =
                                    $"Uploaded {FormatBytes(progress.BytesRead)} out of {FormatBytes(progress.TotalBytesExpected ?? 0)}.";
                                Status = FileUpDownProgressText;
                                FileUpDownProgress = progress.Percentage;
                            });
                            var response = await _httpClient.PostWithProgressAsync(
                                uploadUrl,
                                content,
                                progress
                            );
                            Status = response.IsSuccessStatusCode

                                ? $"File uploaded successfully."
                                : $"Failed to upload File.";
                            await ShowNotificationAsync(Status);
                            DownloadedFiles.RemoveAt(SelectedDownloadedFileIndex);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Status = $"Error uploading File: {ex.Message}";
                        await ShowNotificationAsync(Status);
                        allUploadsSuccessful = false;
                    }
                }

                return allUploadsSuccessful;
            }
            if (DownloadedFiles.Count > 0)
            {
                HasFilesDownloaded = !true;
            }
            else
            {
                HasFilesDownloaded = !false;
            }
            return false;
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
                    // _fileProcess.EnableRaisingEvents = true;
                    // _fileProcess.Exited += (sender, args) =>
                    // {
                    //     IsAlreadyProcessing = false;
                    // };
                    _fileProcess.Start();
                    //_timer.Start();
                    DownloadedFiles.Add(
                        new Downloads()
                        {
                            FileName = new FileInfo(filePath).Name,
                            FilePath = filePath,
                            FileTime = File.GetLastWriteTime(filePath)
                        });
                    if (DownloadedFiles.Count > 0)
                    {
                        HasFilesDownloaded = !true;
                    }
                    else
                    {
                        HasFilesDownloaded = !false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
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
                    Header = "Maximize",
                    Command = new RelayCommand(async () =>
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            mainWindow.WindowState = WindowState.Maximized;
                            mainWindow.ShowInTaskbar = true;
                        });
                    })
                }
            );
            _trayMenu.Add(
                new NativeMenuItem
                {
                    Header = "Upload all edited files",
                    Command = new RelayCommand(async () =>
                    {
                        UploadFiles(ignoreIndex: true);
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
                Icon = new(AppDomain.CurrentDomain.BaseDirectory + "icon.ico"),
                IsVisible = true,
                ToolTipText = "Url Handler",
                Menu = _trayMenu
            };
            // wire up events
            _notifyIcon.Clicked += (sender, e) => ShowMainWindow();
        }

        private async Task ShowNotificationAsync(string body, string title = "Status")
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
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
