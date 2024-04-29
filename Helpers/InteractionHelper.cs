using Avalonia.Controls;
using System;
using urlhandler.ViewModels;

namespace urlhandler.Helpers {
  internal static class InteractionHelper {
    internal static void ResetLastInteractionTime(MainWindowViewModel mainWindowView) {
      mainWindowView.lastInteractionTime = DateTime.Now;
      if (mainWindowView.isMinimizedByIdleTimer) {
        mainWindowView.mainWindow.WindowState = WindowState.Normal;
        mainWindowView.mainWindow.ShowInTaskbar = true;
        mainWindowView.isMinimizedByIdleTimer = false;
        if (mainWindowView.idleTimer != null) {
          mainWindowView.idleTimer.IsEnabled = true;
          mainWindowView.idleTimer.Start();
        }
        else {
          Console.WriteLine("Error: idleTimer is null.");
        }
        mainWindowView._notificationHelper.ShowNotificationAsync("The window has been restored after being idle. You can continue your work.", mainWindowView, "Window Restored").Wait();
      }
    }
  }
}
