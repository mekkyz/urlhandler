using Avalonia.Threading;
using System;
using System.Threading;
using urlhandler.Services.Abstract;
using urlhandler.ViewModels;

namespace urlhandler.Services.Concrete {
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
          mainWindowView.FileUpDownProgressText = $"Downloaded {mainWindowView._byteService.FormatBytes(progress.BytesRead)} out of {mainWindowView._byteService.FormatBytes(progress.TotalBytesExpected ?? 0)} for file {fileId}.";
          mainWindowView.FileUpDownProgress = progress.Percentage;
        });
      }, mainWindowView, 300);
    }
  }
}
