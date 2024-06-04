using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using urlhandler.Models;
using urlhandler.ViewModels;
using urlhandler.Extensions;
using urlhandler.Helpers;

namespace urlhandler.Services;
public interface IUploadService {
  Task<bool> UploadFiles(MainWindowViewModel mainWindowView, bool ignoreIndex = false);
}
internal class UploadService : IUploadService {
  public async Task<bool> UploadFiles(MainWindowViewModel mainWindowView, bool ignoreIndex = false) {
    bool allUploadsSuccessful = true;

    try {
      List<Downloads> previouslyUploadedFiles = mainWindowView.DownloadedFiles.ToList();
      if (mainWindowView.DownloadedFiles.Count > 0) {
        for (int i = mainWindowView.DownloadedFiles.Count - 1; i >= 0; i--) {
          string filePath = mainWindowView.DownloadedFiles[i].FilePath;
          if (filePath == null || mainWindowView.AuthToken == null)
            return false;
          string uploadUrl = ApiHelper.UploadUrl(mainWindowView.AuthToken);
          DateTime lastWriteTime = File.GetLastWriteTime(filePath);
          Downloads previouslyUploadedFile =
            previouslyUploadedFiles.Find(f => f.FilePath == filePath)
            ?? throw new InvalidOperationException("File not found.");

          if (!ignoreIndex && previouslyUploadedFile != null && lastWriteTime <= previouslyUploadedFile.FileTime) {
            mainWindowView.Status = $"File {i + 1} ({new FileInfo(filePath).Name}) has not been modified since the last upload. Skipping...";
            await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
            continue;
          }

          byte[] fileContentBytes = await File.ReadAllBytesAsync(filePath);

          var content = new MultipartFormDataContent {
              { new ByteArrayContent(fileContentBytes), "file", Path.GetFileName(filePath) }
          };
          var fileName = new FileInfo(filePath).Name;

          content.Add(new StringContent(fileName), "attachmentName");

          var progress = new Progress<ProgressInfo>(progress => {
            mainWindowView.FileUpDownProgressText = $"Uploaded {progress.BytesRead.FormatBytes()} out of {progress.TotalBytesExpected?.FormatBytes() ?? "0"}.";
            mainWindowView.Status = mainWindowView.FileUpDownProgressText;
            mainWindowView.FileUpDownProgress = progress.Percentage;
          });
          var response = await mainWindowView._httpClient.PostWithProgressAsync(uploadUrl, content, progress);

          mainWindowView.Status = response.IsSuccessStatusCode ? "File uploaded successfully." : "Failed to upload file.";

          await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

          mainWindowView.DownloadedFiles.RemoveAt(i);
        }
      }
    }

    catch (Exception ex) {
      mainWindowView.Status = $"Error uploading files: {ex.Message}";
      await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
      allUploadsSuccessful = false;
    }

    if (mainWindowView.DownloadedFiles.Count < 1) {
      mainWindowView.HasFilesDownloaded = false;
    }
    else {
      mainWindowView.HasFilesDownloaded = true;
    }

    return allUploadsSuccessful;
  }
}
