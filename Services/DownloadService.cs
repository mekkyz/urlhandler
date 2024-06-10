using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using urlhandler.Extensions;
using urlhandler.Helpers;
using urlhandler.Models;
using urlhandler.ViewModels;

namespace urlhandler.Services;

internal interface IDownloadService {
  Task<string?> DownloadFile(MainWindowViewModel mainWindowView, string token);
}

internal class DownloadService : IDownloadService {

  public async Task<string?> DownloadFile(MainWindowViewModel mainWindowView, string authToken = "") {
    try {

      var token = authToken.Length < 1 ? mainWindowView.Url[(mainWindowView.Url.LastIndexOf('/') + 1)..] : authToken;
      mainWindowView.AuthToken = token;
      var url = new Uri(mainWindowView.Url);
      ApiHelper.apiHost = $"{url.Scheme}://{url.Host}";
      /* test if token is valid or expired
      var handler = new JwtSecurityTokenHandler();
      var isValid = JwtValidationHelper.IsTokenValid(token);*/
      var downloadUrl = ApiHelper.DownloadUrl(token);
      #region Get new token, use it when an oven fresh token is needed!
      /*#if DEBUG
      await _tokenService.FetchAuthToken(mainWindowView);
      var dummy = "Array.Empty<String;";
      #endif*/
      #endregion
      mainWindowView.Status = "Downloading File...";
      await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
      var progress = new Progress<ProgressInfo>(progressInfo => {
        Dispatcher.UIThread.Invoke(() => {
          mainWindowView.FileUpDownProgressText =
            $"Downloaded {progressInfo.BytesRead.FormatBytes()} out of {progressInfo.TotalBytesExpected?.FormatBytes() ?? "0"}.";
          mainWindowView.FileUpDownProgress = progressInfo.Percentage;
          mainWindowView.Status = mainWindowView.FileUpDownProgressText;
        });
      });
      var (response, fileContentBytes) = await mainWindowView._httpClient.GetWithProgressAsync(downloadUrl, progress);
      var headers = response.Content.Headers;

      var _headers = headers.ToImmutableDictionary();
      var contentDisposition = _headers["Content-Disposition"].FirstOrDefault();

      if (!response.IsSuccessStatusCode ||
          contentDisposition == null ||
          !contentDisposition.Contains("filename")) {
        mainWindowView.Status = "Failed to download file.";
        await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

        return null;
      }

      var fileName = contentDisposition[(contentDisposition.IndexOf("=", StringComparison.Ordinal) + 1)..];
      var filePath = Path.Combine(Path.GetTempPath(), fileName);
      var existingDownload = WindowHelper.MainWindowViewModel?.DownloadedFiles.FirstOrDefault(x => x.FilePath == filePath);
      if (existingDownload != null) {
        await NotificationHelper.ShowNotificationAsync("Same file or or same file name already being processed.", mainWindowView);
        return "Already Exists!";
      }

      await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
      await fileStream.WriteAsync(fileContentBytes.AsMemory(0, fileContentBytes.Length));
      return filePath;
    }
    catch (Exception ex) {
      var errorMessage = ex is IOException ?
        $"File access error: {ex.Message} - The file might be in use by another process or locked." :
        $"An error occurred: {ex.Message}";
      mainWindowView.Status = errorMessage;
      await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
      return null;
    }
  }
}
