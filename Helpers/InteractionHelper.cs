using System;
using Avalonia.Controls;
using urlhandler.ViewModels;

namespace urlhandler.Helpers;

internal static class InteractionHelper {
  internal static void ResetLastInteractionTime(MainWindowViewModel mainWindowView) {
    mainWindowView.lastInteractionTime = DateTime.Now;

    if (!mainWindowView.isMinimizedByIdleTimer) {
      return;
    }

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

    NotificationHelper.ShowNotificationAsync(
      "The window has been restored after being idle. You can continue your work.",
      mainWindowView,
      "Window Restored"
    ).Wait();
  }
}
