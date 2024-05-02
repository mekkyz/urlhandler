using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System;
using System.Threading.Tasks;
using urlhandler.Models;
using urlhandler.Services.Abstract;
using urlhandler.ViewModels;
using System.Linq;

namespace urlhandler.Services.Concrete {
  internal class UploadService : IUploadService {
    public async Task<bool> UploadFiles(MainWindowViewModel mainWindowView, bool ignoreIndex = false) {
      bool allUploadsSuccessful = true;

      try {
        // get list of uploaded files
        List<Downloads> previouslyUploadedFiles = mainWindowView.DownloadedFiles.ToList();

        if (mainWindowView.DownloadedFiles.Count > 0 && ignoreIndex) {
          // upload all files that were modified
          for (int i = mainWindowView.DownloadedFiles.Count - 1; i >= 0; i--) {
            string filePath = mainWindowView.DownloadedFiles[i].FilePath;

            if (filePath == null || mainWindowView.AuthToken == null)
              return false;

            string uploadUrl = $"http://127.0.0.1:3000/upload?authtoken={mainWindowView.AuthToken}";

            // check if file modified since last upload
            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
            Downloads previouslyUploadedFile =
              previouslyUploadedFiles.Find(f => f.FilePath == filePath)
              ?? throw new InvalidOperationException("File not found.");
            if (previouslyUploadedFile != null && lastWriteTime < previouslyUploadedFile.FileTime) {
              mainWindowView.Status = $"File {i + 1} ({new FileInfo(filePath).Name}) has not been modified since the last upload. Skipping...";
              await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
              continue;
            }

            byte[] fileContentBytes = await File.ReadAllBytesAsync(filePath);
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileContentBytes), "file", Path.GetFileName(filePath));
            var progress = new Progress<ProgressInfo>(progress => {
              mainWindowView.FileUpDownProgressText = $"Uploaded {mainWindowView._byteService.FormatBytes(progress.BytesRead)} out of {mainWindowView._byteService.FormatBytes(progress.TotalBytesExpected ?? 0)}.";
              mainWindowView.Status = mainWindowView.FileUpDownProgressText;
              mainWindowView.FileUpDownProgress = progress.Percentage;
            });
            var response = await mainWindowView._httpClient.PostWithProgressAsync(uploadUrl, content, progress);
            mainWindowView.Status = response.IsSuccessStatusCode ? "File uploaded successfully." : "Failed to upload file.";
            await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
            mainWindowView.DownloadedFiles.RemoveAt(i);
            //WindowHelper.MainWindow.lBoxEditedFiles.ItemsSource = new ObservableCollection<Downloads>(mainWindowView.DownloadedFiles.Where(x => File.GetLastWriteTime(x.FilePath) > File.GetCreationTime(x.FilePath)).ToList() as List<Downloads>);
          }
        }
      }
      catch (Exception ex) {
        mainWindowView.Status = $"Error uploading files: {ex.Message}";
        await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
        allUploadsSuccessful = false;
      }
      return allUploadsSuccessful;
    }

    public async Task<bool> UploadFiles(MainWindowViewModel mainWindowView) {
      bool allUploadsSuccessful = true;

      try {
        if (mainWindowView.DownloadedFiles == null || mainWindowView.DownloadedFiles.Count < 1) {
          return false;
        }

        List<Downloads> previouslyUploadedFiles = mainWindowView.DownloadedFiles.ToList();

        // upload all files that have been modified
        for (int i = mainWindowView.DownloadedFiles.Count - 1; i >= 0; i--) {
          string filePath = mainWindowView.DownloadedFiles[i].FilePath;

          if (filePath == null || mainWindowView.AuthToken == null)
            return false;

          string uploadUrl = $"http://127.0.0.1:3000/upload?authtoken={mainWindowView.AuthToken}";

          try {
            // check if file modified
            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
            Downloads? previouslyUploadedFile = previouslyUploadedFiles?.Find(f => f.FilePath == filePath);
            if (previouslyUploadedFile != null && lastWriteTime <= previouslyUploadedFile.FileTime) {
              mainWindowView.Status = $"File {i + 1} ({new FileInfo(filePath).Name}) has not been modified since the last upload. Skipping...";
              await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
              continue;
            }

            byte[] fileContentBytes = await File.ReadAllBytesAsync(filePath);
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileContentBytes), "file", Path.GetFileName(filePath));
            var progress = new Progress<ProgressInfo>(progress => {
              mainWindowView.FileUpDownProgressText = $"Uploaded {mainWindowView._byteService.FormatBytes(progress.BytesRead)} out of {mainWindowView._byteService.FormatBytes(progress.TotalBytesExpected ?? 0)}.";
              mainWindowView.Status = mainWindowView.FileUpDownProgressText;
              mainWindowView.FileUpDownProgress = progress.Percentage;
            });
            var response = await mainWindowView._httpClient.PostWithProgressAsync(uploadUrl, content, progress);
            mainWindowView.Status = response.IsSuccessStatusCode ? "File uploaded successfully." : "Failed to upload file.";
            await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
            mainWindowView.DownloadedFiles.RemoveAt(i);
            //WindowHelper.MainWindow.lBoxEditedFiles.ItemsSource = new ObservableCollection<Downloads>(mainWindowView.DownloadedFiles.Where(x => File.GetLastWriteTime(x.FilePath) > File.GetCreationTime(x.FilePath)).ToList() as List<Downloads>);
          }
          catch (Exception ex) {
            mainWindowView.Status = $"Error uploading File {i + 1}: {ex.Message}";
            await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
            allUploadsSuccessful = false;
          }
        }
        if (mainWindowView.DownloadedFiles.Count < 1) {
          mainWindowView.HasFilesDownloaded = false;
        }
        else {
          mainWindowView.HasFilesDownloaded = true;
        }
      }
      catch (Exception ex) {
        mainWindowView.Status = $"Error uploading files: {ex.Message}";
        await mainWindowView._notificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
        allUploadsSuccessful = false;
      }
      return allUploadsSuccessful;
    }
  }
}
