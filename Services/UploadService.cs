using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
      if (WindowHelper.MainWindowViewModel.SelectedDownloadedFileIndex > -1) {
        var file = WindowHelper.MainWindowViewModel.DownloadedFiles[
          WindowHelper.MainWindowViewModel.SelectedDownloadedFileIndex];
        var fileSumOnDisk = file.FilePath.FileCheckSum();
        var fileSumOnDownload = file.FileSumOnDownload;

        if (!fileSumOnDisk.Equals(fileSumOnDownload)) {
          var upload = await Upload(file.FilePath, WindowHelper.MainWindowViewModel);
          if (!upload) return false;
          WindowHelper.MainWindowViewModel.DownloadedFiles.RemoveAt(WindowHelper.MainWindowViewModel
            .SelectedDownloadedFileIndex);
          Debug.WriteLine(WindowHelper.MainWindowViewModel.DownloadedFiles.Count);
          return true;

        }

        WindowHelper.MainWindowViewModel.Status = FeedbackHelper.FileNotEdited;
        await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel.Status, WindowHelper.MainWindowViewModel);
        return false;
      }

      {
        var tempList = WindowHelper.MainWindowViewModel.DownloadedFiles.ToList();
        var filesToRemove = new ObservableCollection<Downloads>();

        foreach (var file in tempList) {
          var fileSumOnDisk = file.FilePath.FileCheckSum();
          var fileSumOnDownload = file.FileSumOnDownload;

          if (fileSumOnDisk.Equals(fileSumOnDownload)) continue;
          var upload = await Upload(file.FilePath, WindowHelper.MainWindowViewModel);
          if (upload) {
            filesToRemove.Add(file);
          }
        }

        if (filesToRemove.Count > 0) {
          foreach (var file in filesToRemove) {
            WindowHelper.MainWindowViewModel.DownloadedFiles.Remove(file);
          }
          var path = $"{AppDomain.CurrentDomain.BaseDirectory}downloads.json";

          var data = JsonConvert.SerializeObject(WindowHelper.MainWindowViewModel.DownloadedFiles);
          await File.WriteAllTextAsync(path, data);
          return true;
        }
      }

      WindowHelper.MainWindowViewModel.Status = FeedbackHelper.FileNotEdited;
      await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel.Status, WindowHelper.MainWindowViewModel);
      return false;
    }

    WindowHelper.MainWindowViewModel!.Status = FeedbackHelper.NoDownloads;
    await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel.Status, WindowHelper.MainWindowViewModel);
    return false;
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
          if (prog.BytesRead >= new FileInfo(filePath).Length) {
            mainView.Status = FeedbackHelper.UploadSuccessful;
          }
        });

        var response = await mainView._httpClient.PostWithProgressAsync(ApiHelper.UploadUrl(mainView.AuthToken),
          content, progress, isUpload: true);

        if (response.IsSuccessStatusCode) {
          mainView.Status = FeedbackHelper.UploadSuccessful;
          await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel?.Status!, mainView);
          return true;
        }
        else {
          mainView.Status = FeedbackHelper.UploadFail;
          await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel?.Status!, mainView);
          return false;
        }
      }

      mainView.Status = FeedbackHelper.FileAccessError;
      await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel?.Status!, mainView);
      return false;
    }
    catch (Exception) {
      mainView.Status = FeedbackHelper.UploadFail;
      await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel?.Status!, mainView);
      return false;
    }
  }
}
