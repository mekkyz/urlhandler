using Avalonia;
using DesktopNotifications;
using DesktopNotifications.Avalonia;

namespace urlhandler {

    internal class Program {
        public static INotificationManager NotificationManager = null!;

        private static void Main(string[] args) {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
                .UsePlatformDetect()
            .SetupDesktopNotifications(out NotificationManager!)
            .LogToTrace();
    }
}