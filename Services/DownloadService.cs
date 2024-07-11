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
  Task<(string filePath, string originalName)?> DownloadFile(MainWindowViewModel mainWindowView, string token);
}

internal class DownloadService : IDownloadService {

  public async Task<(string filePath, string originalName)?> DownloadFile(MainWindowViewModel mainWindowView, string authToken) {
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
      var fileDir = Path.Combine(Path.GetTempPath(), "chemotion");
      Directory.CreateDirectory(fileDir);

      var filePath = Path.Combine(fileDir, fileName);
      var originalName = Path.GetFileName(filePath);
      var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
      var fileExtension = Path.GetExtension(fileName);
      var counter = 1;

      while (File.Exists(filePath)) {
        filePath = Path.Combine(fileDir, $"{fileNameWithoutExtension}-{counter}{fileExtension}");
        counter++;
      }

      await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
      await fileStream.WriteAsync(fileContentBytes.AsMemory(0, fileContentBytes.Length));
      return (filePath, originalName);
    }
    catch (Exception ex) {
      var errorMessage = ex is IOException ? FeedbackHelper.FileAccessError : FeedbackHelper.UnExpectedError;
      mainWindowView.Status = errorMessage;
      await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
      return null;
    }
  }
}
