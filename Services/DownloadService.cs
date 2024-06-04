using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using urlhandler.ViewModels;
using Avalonia.Threading;
using urlhandler.Extensions;
using urlhandler.Helpers;
using urlhandler.Models;
using System.Threading.Tasks;

namespace urlhandler.Services.Concrete;

internal interface IDownloadService {
  Task<string?> DownloadFile(MainWindowViewModel mainWindowView, string token);
  Task DownloadFilesConcurrently(IList<string> fileIds, string authtoken, MainWindowViewModel mainWindowView);
}


internal class DownloadService : IDownloadService {
  TokenService _tokenService;
  public DownloadService() => _tokenService = new TokenService();
  public async Task<string?> DownloadFile(MainWindowViewModel mainWindowView, string authToken = "") {
    try {

      var token = authToken.Length < 1 ? mainWindowView.Url.Substring(mainWindowView.Url.LastIndexOf('/') + 1) : authToken;

      mainWindowView.AuthToken = token;

      var _url = new Uri(mainWindowView.Url);

      ApiHelper.apiHost = $"{_url.Scheme}://{_url.Host}";

      string downloadUrl = ApiHelper.DownloadUrl(token);

      mainWindowView.Status = "Downloading File...";

      await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

      var progress = new Progress<ProgressInfo>(progressInfo => {
        Dispatcher.UIThread.Invoke(() => {
          mainWindowView.FileUpDownProgressText =
            $"Downloaded {progressInfo.BytesRead.FormatBytes()} out of {progressInfo.TotalBytesExpected?.FormatBytes() ?? "0"}.";
          mainWindowView.FileUpDownProgress = progressInfo.Percentage;
          mainWindowView.Status = mainWindowView.FileUpDownProgressText;
        });
      });

      var (response, fileContentBytes) = await mainWindowView._httpClient.GetWithProgressAsync(downloadUrl, progress);

      Debug.WriteLine(response);

      HttpContentHeaders headers = response.Content.Headers;

      var _headers = headers.ToImmutableDictionary();
      var contentDisposition = _headers["Content-Disposition"].FirstOrDefault();

      if (!response.IsSuccessStatusCode ||
          contentDisposition == null ||
          !contentDisposition.Contains("filename")) {
        mainWindowView.Status = "Failed to download file.";
        await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

        return null;
      }

      string fileName = contentDisposition.Substring(contentDisposition.IndexOf("=", StringComparison.Ordinal) + 1) ?? "";
      string filePath = Path.Combine(Path.GetTempPath(), fileName);
      using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) {
        await fileStream.WriteAsync(fileContentBytes.AsMemory(0, fileContentBytes.Length));
      }
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

  public async Task DownloadFilesConcurrently(IList<string> fileIds, string authtoken, MainWindowViewModel mainWindowView) {
    var downloadTasks = fileIds.Select(fileId => mainWindowView._downloadService.DownloadFile(mainWindowView));
    var files = await Task.WhenAll(downloadTasks);

    foreach (var file in files.Where(f => f != null)) {
      mainWindowView._fileService.ProcessFile(file, mainWindowView);
    }
  }
}
