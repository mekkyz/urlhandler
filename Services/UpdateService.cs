using System;
using System.Threading;
using Avalonia.Threading;
using urlhandler.Extensions;
using urlhandler.Models;
using urlhandler.ViewModels;

namespace urlhandler.Services.Concrete;
internal interface IUpdateService {
  void DebounceUpdate(Action updateAction, MainWindowViewModel mainWindowView, int interval = 300);
  void UpdateProgress(ProgressInfo progress, string fileId, MainWindowViewModel mainWindowView);
}
internal class UpdateService : IUpdateService {
  public void DebounceUpdate(Action updateAction, MainWindowViewModel mainWindowView, int interval = 300) {
    mainWindowView._debounceTimer?.Dispose();
    mainWindowView._debounceTimer = new Timer(_ => {
      updateAction();
      mainWindowView._debounceTimer = null;
    }, null, interval, Timeout.Infinite);
  }

  public void UpdateProgress(ProgressInfo progress, string fileId, MainWindowViewModel mainWindowView) {
    DebounceUpdate(() => {
      Dispatcher.UIThread.Invoke(() => {
        mainWindowView.FileUpDownProgressText = $"Downloaded {progress.BytesRead.FormatBytes()} out of {progress.TotalBytesExpected?.FormatBytes() ?? "0"} for file {fileId}.";
        mainWindowView.FileUpDownProgress = progress.Percentage;
      });
    }, mainWindowView, 300);
  }
}
