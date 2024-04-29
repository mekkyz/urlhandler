using System.IO;
using System;
using System.Threading.Tasks;
using urlhandler.Services.Abstract;
using urlhandler.ViewModels;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Linq;

namespace urlhandler.Services.Concrete {
  internal class DownloadService : IDowloadService {
    TokenService _tokenService;
    public DownloadService() {
      _tokenService = new TokenService();
    }
    public async Task<string?> DownloadFile(string? fileId, int authtoken, MainWindowViewModel mainWindowView) {
      try {
        if (fileId == null) {
          mainWindowView.Status = "FileId is required.";
          await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return null;
        }
        if (authtoken != mainWindowView.AuthToken) {
          mainWindowView.Status = "Failed to download with old token. Please use a fresh token.";
          await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return null;
        }
        string downloadUrl = $"http://127.0.0.1:3000/download?fileId={fileId}&authtoken={authtoken}";
        mainWindowView.Status = "Downloading File...";
        await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
        var progress = new Progress<ProgressInfo>(progressInfo => {
          Dispatcher.UIThread.Invoke(() => {
            mainWindowView.FileUpDownProgressText = $"Downloaded {mainWindowView._byteService.FormatBytes(progressInfo.BytesRead)} out of {mainWindowView._byteService.FormatBytes(progressInfo.TotalBytesExpected ?? 0)}.";
            mainWindowView.FileUpDownProgress = progressInfo.Percentage;
            mainWindowView.Status = mainWindowView.FileUpDownProgressText;
          });
        });
        var (response, fileContentBytes) = await mainWindowView._httpClient.GetWithProgressAsync(downloadUrl, progress);
        if (!response.IsSuccessStatusCode) {
          mainWindowView.Status = "Failed to download file.";
          await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return null;
        }

        string filePath = Path.Combine(Path.GetTempPath(), fileId);
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) {
          await fileStream.WriteAsync(fileContentBytes.AsMemory(0, fileContentBytes.Length));
        }

        await _tokenService.FetchAuthToken(mainWindowView);

        mainWindowView.Status = "File downloaded successfully.";
        await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
        return filePath;
      }
      catch (Exception ex) {
        string errorMessage = ex is IOException ?
            $"File access error: {ex.Message} - The file might be in use by another process or locked." :
            $"An error occurred: {ex.Message}";

        mainWindowView.Status = errorMessage;
        await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

        return null;
      }
    }

    public async Task DownloadFilesConcurrently(IEnumerable<string> fileIds, int authtoken, MainWindowViewModel mainWindowView) {
      var downloadTasks = fileIds.Select(fileId => mainWindowView._downloadService.DownloadFile(fileId, authtoken, mainWindowView));
      var files = await Task.WhenAll(downloadTasks);
      foreach (var file in files.Where(f => f != null)) {
        mainWindowView._fileService.ProcessFile(file, mainWindowView);
      }
    }
  }
}
