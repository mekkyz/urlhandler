using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Web;

namespace urlhandler {

    public partial class MainWindow : Window {
        private TrayIcon? _notifyIcon;
        private HttpClient _httpClient = new HttpClient();
        private DateTime lastWriteTimeBeforeProcessing;
        private Timer _timer;
        private bool _isFileChanged;
        private string? _filePath;
        private string? _authToken;
        private string? _fileId;
        private INotificationManager? toast;
        private DispatcherTimer? idleTimer;
        private DateTime lastInteractionTime;
        private bool isMinimizedByIdleTimer = false;
        private Notification nf = new Notification();

        public MainWindow() {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            _timer = new Timer(1000); // check every second for file changes
            _timer.Elapsed += async (sender, e) => await CheckFileChanges();
            InitializeSystemTrayIcon();
        }

        private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            toast = Program.NotificationManager ??
                                   throw new InvalidOperationException("Missing notification toast");
            MinimizeWindowOnIdle();

            Deactivated += MainWindow_Deactivated;
        }

        // checks for window state if minimized by user
        private void MainWindow_Deactivated(object? sender, EventArgs e) {
            if (isMinimizedByIdleTimer == false && WindowState == WindowState.Minimized) {
                ShowInTaskbar = false;
                idleTimer!.IsEnabled = false;
                idleTimer.Stop();
                isMinimizedByIdleTimer = false;
            } else {
                ShowInTaskbar = true;
                idleTimer!.IsEnabled = true;
                idleTimer.Start();
                isMinimizedByIdleTimer = false;
            }
        }

        #region MinimizingWindowAfterbeingIdle

        // inactivity threshold
        private void MinimizeWindowOnIdle() {
            var window = this;
            idleTimer = new DispatcherTimer();
            idleTimer.Interval = TimeSpan.FromSeconds(5);
            idleTimer.Tick += (sender, e) => {
                var elapsedTime = DateTime.Now - lastInteractionTime;
                if (elapsedTime.TotalSeconds > 5)
                {
                    isMinimizedByIdleTimer = true;
                    this.WindowState = WindowState.Minimized;
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

        private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e) {
            // reset interaction on key press
            lastInteractionTime = DateTime.Now;
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e) {
            // reset interaction on mouse move
            lastInteractionTime = DateTime.Now;
        }

        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e) {
            // reset interaction on mouse click
            lastInteractionTime = DateTime.Now;
        }

        private void OnActivated(object sender, EventArgs e) {
            // reset interaction on window focus
            lastInteractionTime = DateTime.Now;
        }

        #endregion MinimizingWindowAfterbeingIdle

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnProcessButtonClick(object sender, RoutedEventArgs e) {
            var uriBox = this.FindControl<TextBox>("UriBox")!;
            var statusBox = this.FindControl<TextBlock>("StatusBox")!;

            string uri = uriBox.Text!;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsedUri)) {
                statusBox.Text = "Invalid URL format.";
                await ShowNotificationAsync(statusBox.Text);
                return;
            }

            var queryParams = HttpUtility.ParseQueryString(parsedUri.Query);
            _fileId = queryParams["fileId"];
            /*
            _authToken = queryParams["auth"];
            */

            if (string.IsNullOrEmpty(_fileId)) {
                statusBox.Text = "FileId is required.";
                await ShowNotificationAsync(statusBox.Text); //
                return;
            }

            _filePath = await DownloadFile(_fileId);
            if (_filePath == null) {
                statusBox.Text = "Failed to download file.";

                await ShowNotificationAsync(statusBox.Text);

                return;
            }

            ProcessFile(_filePath);
        }

        private async Task<string?> DownloadFile(string? fileId) {
            if (fileId == null)
                return null;

            string downloadUrl = $"http://127.0.0.1:3000/download?fileId={fileId}"; ;
            try {
                var statusBox = this.FindControl<TextBlock>("StatusBox")!;
                statusBox.Text = "Downloading File...";
                await ShowNotificationAsync(statusBox.Text);

                var response = await _httpClient.GetAsync(downloadUrl);
                statusBox.Text = response.IsSuccessStatusCode ? "File downloaded successfully." : "Failed to download file.";
                await ShowNotificationAsync(statusBox.Text);
                if (!response.IsSuccessStatusCode) {
                    return null;
                }

                byte[] fileContentBytes = await response.Content.ReadAsByteArrayAsync();

                string filePath = Path.Combine(Path.GetTempPath(), fileId);

                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
                    await fileStream.WriteAsync(fileContentBytes, 0, fileContentBytes.Length);
                }
                return filePath;
            } catch {
                return null;
            }
        }

        private void ProcessFile(string? filePath) {
            if (filePath != null) {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                _timer.Start();
                lastWriteTimeBeforeProcessing = File.GetLastWriteTime(filePath);
            }
        }

        private async Task<bool> UploadFile(string? filePath) {
            /*if (filePath == null || authToken == null)
                return false;*/
            if (filePath == null) return false;

            string uploadUrl = $"http://127.0.0.1:3000/upload";
            try {
                byte[] fileContentBytes = File.ReadAllBytes(filePath);

                var content = new MultipartFormDataContent();

                // only for test api. 'file' parameter to post for uploading
                content.Add(new ByteArrayContent(fileContentBytes), name: "file", new FileInfo(filePath).Name);

                var response = await _httpClient.PostAsync(uploadUrl, content);
                return response.IsSuccessStatusCode;
            } catch {
                return false;
            }
        }

        private async Task CheckFileChanges() {
            /*if (_filePath == null || _fileId == null || _authToken == null)
                return;*/
            if (_filePath == null || _fileId == null)
                return;

            DateTime lastWriteTimeAfterProcessing = File.GetLastWriteTime(_filePath);
            if (lastWriteTimeBeforeProcessing != lastWriteTimeAfterProcessing) {
                _isFileChanged = true;
                _timer.Stop(); // stop monitoring

                await Dispatcher.UIThread.InvokeAsync(async () => {
                    var statusBox = this.FindControl<TextBlock>("StatusBox")!;
                    statusBox.Text = "File has been modified, uploading...";
                    await ShowNotificationAsync(statusBox.Text);
                    bool uploadSuccess = await UploadFile(_filePath);
                    statusBox.Text = uploadSuccess ? "File uploaded successfully." : "Failed to upload file.";
                    await ShowNotificationAsync(statusBox.Text);
                });
            }
        }

        private void InitializeSystemTrayIcon() {
            var _trayMenu = new NativeMenu();

            _trayMenu.Add(new NativeMenuItem {
                Header = "Finish Editing and Upload File Now",
                Command = new RelayCommand(async () => {
                    if (_isFileChanged && _filePath != null) {
                        var statusBox = this.FindControl<TextBlock>("StatusBox")!;
                        statusBox.Text = "Uploading file...";
                        await ShowNotificationAsync(statusBox.Text);
                        bool uploadSuccess = await UploadFile(_filePath);
                        statusBox.Text = uploadSuccess ? "File uploaded successfully." : "Failed to upload file.";
                        await ShowNotificationAsync(statusBox.Text);
                    }
                })
            });

            _trayMenu.Add(new NativeMenuItem {
                Header = "Exit",
                Command = new RelayCommand(() => {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp) {
                        desktopApp.Shutdown();
                    }
                })
            });

            _notifyIcon = new TrayIcon {
                Icon = new("icon.ico"),
                IsVisible = true,
                ToolTipText = "Url Handler from Mostafa",
                Menu = _trayMenu
            };

            // events wire up
            _notifyIcon.Clicked += (sender, e) => ShowMainWindow();
        }

        private void ShowMainWindow() {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
        }

        protected override void OnClosed(EventArgs e) {
            _notifyIcon!.Dispose();
            base.OnClosed(e);
        }

        private async void OnUploadButtonClick(object sender, RoutedEventArgs e) {
            if (_isFileChanged && _filePath != null) {
                var statusBox = this.FindControl<TextBlock>("StatusBox")!;
                statusBox.Text = "Uploading file...";
                await ShowNotificationAsync(statusBox.Text);
                bool uploadSuccess = await UploadFile(_filePath);
                statusBox.Text = uploadSuccess ? "File uploaded successfully." : "Failed to upload file.";
                await ShowNotificationAsync(statusBox.Text);
            }
        }

        private async Task ShowNotificationAsync(string body, string title = "Status") {
            if (this.WindowState == WindowState.Minimized) {
                nf = new Notification {
                    Title = title,
                    Body = body
                };
                await toast!.ShowNotification(nf);
            }
        }
    }
}