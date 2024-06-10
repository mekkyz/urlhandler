using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
  Task<bool> UploadEditedFiles();
}

internal class UploadService : IUploadService {
  public async Task<bool> UploadEditedFiles() {
    if (WindowHelper.MainWindowViewModel?.DownloadedFiles.Count > 0) {
      if (WindowHelper.MainWindowViewModel?.SelectedDownloadedFileIndex > -1) {
        var file = WindowHelper.MainWindowViewModel.DownloadedFiles[
          WindowHelper.MainWindowViewModel.SelectedDownloadedFileIndex];
        var fileSumOnDisk = file.FilePath.FileCheckSum();
        var fileSumOnDownload = file.FileSumOnDownload;

        if (!fileSumOnDisk.Equals(fileSumOnDownload)) {
          var upload = await Upload(file.FilePath, WindowHelper.MainWindowViewModel);
          if (upload) {
            WindowHelper.MainWindowViewModel.DownloadedFiles.RemoveAt(WindowHelper.MainWindowViewModel
              .SelectedDownloadedFileIndex);
            Debug.WriteLine(WindowHelper.MainWindowViewModel?.DownloadedFiles.Count);
          }
        }
        else {
          await NotificationHelper.ShowNotificationAsync("Selected file hasn't been edited yet.", WindowHelper.MainWindowViewModel);
          return false;
        }

        await NotificationHelper.ShowNotificationAsync("Selected file hasn't been edited yet.", WindowHelper.MainWindowViewModel!);
        return false;
      }
      else {
        if (WindowHelper.MainWindowViewModel?.DownloadedFiles != null) {
          var tempList = WindowHelper.MainWindowViewModel.DownloadedFiles.ToList();
          var filesToRemove = new ObservableCollection<Downloads>();

          foreach (var file in tempList) {
            var fileSumOnDisk = file.FilePath.FileCheckSum();
            var fileSumOnDownload = file.FileSumOnDownload;

            if (!fileSumOnDisk.Equals(fileSumOnDownload)) {
              var upload = await this.Upload(file.FilePath, WindowHelper.MainWindowViewModel);
              if (upload) {
                filesToRemove.Add(file);
              }
            }
          }

          foreach (var file in filesToRemove) {
            WindowHelper.MainWindowViewModel.DownloadedFiles.Remove(file);
          }
          return true;
        }

        else {
          await NotificationHelper.ShowNotificationAsync("No file has been downloaded yet.",
            WindowHelper.MainWindowViewModel!);
          return false;
        }
      }
    }
    else {
      await NotificationHelper.ShowNotificationAsync("Download something first.", WindowHelper.MainWindowViewModel!);
      return false;
    }
  }

  public async Task<bool> Upload(string filePath, MainWindowViewModel mainView) {
    try {
      if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath)) {
        byte[] fileContentBytes = await File.ReadAllBytesAsync(filePath);

        var content = new MultipartFormDataContent {
          { new ByteArrayContent(fileContentBytes), "file", Path.GetFileName(filePath) }
        };
        var fileName = new FileInfo(filePath).Name;
        content.Add(new StringContent(fileName), "attachmentName");

        var fileSize = new FileInfo(filePath).Length.FormatBytes();
        var progress = new Progress<ProgressInfo>(prog => {
          var currentProgress = prog.BytesRead > new FileInfo(filePath).Length
            ? fileSize
            : prog.BytesRead.FormatBytes();
          mainView.FileUpDownProgressText = $"Uploaded {currentProgress} out of {fileSize}.";
          mainView.Status = mainView.FileUpDownProgressText;
          mainView.FileUpDownProgress = prog.Percentage;
        });

        var response = await mainView._httpClient.PostWithProgressAsync(ApiHelper.UploadUrl(mainView.AuthToken),
          content, progress, isUpload: true);

        if (response.IsSuccessStatusCode) {
          mainView.Status = "File uploaded successfully.";
          await NotificationHelper.ShowNotificationAsync(mainView.Status, mainView);
          return true;
        }
        else {
          mainView.Status = "Upload failed.";
          await NotificationHelper.ShowNotificationAsync(mainView.Status, mainView);
          return false;
        }
      }

      mainView.Status = "File path is invalid or file does not exist.";
      await NotificationHelper.ShowNotificationAsync(mainView.Status, mainView);
      return false;
    }
    catch (Exception) {
      mainView.Status = "An error occurred during upload.";
      await NotificationHelper.ShowNotificationAsync(mainView.Status, mainView);
      return false;
    }
  }
}
