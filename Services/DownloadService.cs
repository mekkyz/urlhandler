using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

      var token = authToken.Length < 1 ? mainWindowView.Url![(mainWindowView.Url!.LastIndexOf('/') + 1)..] : authToken;
      mainWindowView.AuthToken = token;
      var url = new Uri(mainWindowView.Url!);
      ApiHelper.apiHost = $"{url.Scheme}://{url.Host}";
      var downloadUrl = ApiHelper.DownloadUrl(token);
      mainWindowView.Status = FeedbackHelper.Downloading;
      var progress = new Progress<ProgressInfo>(progressInfo => {
        mainWindowView.Status =
          $"Downloaded {progressInfo.BytesRead.FormatBytes()} out of {progressInfo.TotalBytesExpected?.FormatBytes() ?? "0"}.";
        if (progressInfo.BytesRead >= progressInfo.TotalBytesExpected) {
          if (mainWindowView._filePath == "Already Exists!") {
            mainWindowView.Status = FeedbackHelper.AlreadyExists;
          }
        }
      });

      var (response, fileContentBytes) = await mainWindowView._httpClient.GetWithProgressAsync(downloadUrl, progress);
      var headers = response.Content.Headers;

      var _headers = headers.ToImmutableDictionary();
      var contentDisposition = _headers["Content-Disposition"].FirstOrDefault();

      if (!response.IsSuccessStatusCode ||
          contentDisposition == null ||
          !contentDisposition.Contains("filename")) {
        mainWindowView.Status = FeedbackHelper.DownloadFail;
        await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

        return null;
      }

      var fileName = contentDisposition[(contentDisposition.IndexOf("=", StringComparison.Ordinal) + 1)..];
      var filePath = Path.Combine(Path.GetTempPath(), fileName);
      var existingDownload = WindowHelper.MainWindowViewModel?.DownloadedFiles.FirstOrDefault(x => x.FilePath == filePath);
      if (existingDownload != null) {
        return "Already Exists!";
      }

      await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
      await fileStream.WriteAsync(fileContentBytes.AsMemory(0, fileContentBytes.Length));
      return filePath;
    }
    catch (Exception ex) {
      var errorMessage = ex is IOException ? FeedbackHelper.FileAccessError : FeedbackHelper.UnExpectedError;
      mainWindowView.Status = errorMessage;
      await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
      return null;
    }
  }
}
