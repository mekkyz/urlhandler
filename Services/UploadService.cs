using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MsBox.Avalonia;
using urlhandler.Models;
using urlhandler.ViewModels;
using urlhandler.Extensions;
using urlhandler.Helpers;

namespace urlhandler.Services;

public interface IUploadService {
  Task<bool> UploadEditedFiles(string role = "");
}

internal class UploadService : IUploadService {
  public async Task<bool> UploadEditedFiles(string role = "") {
    try {
      var mainWindowViewModel = WindowHelper.MainWindowViewModel;
      if (mainWindowViewModel?.DownloadedFiles.Count == 0) {
        mainWindowViewModel!.Status = FeedbackHelper.NoDownloads;
        await FeedbackHelper.ShowNotificationAsync(mainWindowViewModel.Status, mainWindowViewModel);
        return false;
      }

      if (mainWindowViewModel?.SelectedDownloadedFileIndex > -1) {
        return await HandleSingleFileUpload(role, mainWindowViewModel);
      }
      else {
        return await HandleMultipleFilesUpload(role, mainWindowViewModel!);
      }
    }
    catch (Exception) {
      return false;
    }
    finally {
      var mainWindowViewModel = WindowHelper.MainWindowViewModel;
      if (mainWindowViewModel?.DownloadedFiles != null) {
        mainWindowViewModel.HasFilesDownloaded = mainWindowViewModel.DownloadedFiles.Count > 0;
      }
    }
  }

  private async Task<bool> HandleSingleFileUpload(string role, MainWindowViewModel mainWindowViewModel) {
    var file = mainWindowViewModel.DownloadedFiles[mainWindowViewModel.SelectedDownloadedFileIndex];

    if (file.IsKept) {
      mainWindowViewModel.Status = FeedbackHelper.FileKept;
      await FeedbackHelper.ShowNotificationAsync(mainWindowViewModel.Status, mainWindowViewModel);
      return false;
    }

    var fileSumOnDisk = file.FilePath.FileCheckSum();
    var fileSumOnDownload = file.FileSumOnDownload;

    if (fileSumOnDisk.Equals(fileSumOnDownload)) {
      mainWindowViewModel.Status = FeedbackHelper.FileNotEdited;
      await FeedbackHelper.ShowNotificationAsync(mainWindowViewModel.Status, mainWindowViewModel);
      return false;
    }

    return await AttemptUpload(file.FilePath, mainWindowViewModel, file.OriginalFileName, role);
  }

  private async Task<bool> HandleMultipleFilesUpload(string role, MainWindowViewModel mainWindowViewModel) {
    var tempList = mainWindowViewModel.DownloadedFiles.ToList();
    var filesToRemove = new ObservableCollection<Downloads>();

    foreach (var file in tempList) {
      if (file.IsKept) {
        mainWindowViewModel.Status = FeedbackHelper.FileKept;
        await FeedbackHelper.ShowNotificationAsync(mainWindowViewModel.Status, mainWindowViewModel);
        continue;
      }

      var fileSumOnDisk = file.FilePath.FileCheckSum();
      var fileSumOnDownload = file.FileSumOnDownload;

      if (!fileSumOnDisk.Equals(fileSumOnDownload)) {
        var upload = await AttemptUpload(file.FilePath, mainWindowViewModel, file.OriginalFileName, role);
        if (upload) {
          filesToRemove.Add(file);
        }
      }
    }

    if (filesToRemove.Count == 0) {
      mainWindowViewModel.Status = FeedbackHelper.FileNotEdited;
      await FeedbackHelper.ShowNotificationAsync(mainWindowViewModel.Status, mainWindowViewModel);
      return false;
    }

    return await HandleFileRemoval(filesToRemove, role, mainWindowViewModel);
  }

  private static bool DeleteFile(Downloads file, MainWindowViewModel mainWindowViewModel) {
    File.Delete(file.FilePath);
    mainWindowViewModel.DownloadedFiles.RemoveAt(mainWindowViewModel.SelectedDownloadedFileIndex);
    JsonHelper.WriteDataToAppData();
    return true;
  }

  private static bool KeepFile(Downloads file, MainWindowViewModel mainWindowViewModel) {
    file.IsEdited = false;
    file.IsKept = true;
    JsonHelper.WriteDataToAppData();
    return true;
  }

  private async Task<bool> HandleFileRemoval(ObservableCollection<Downloads> filesToRemove, string role, MainWindowViewModel mainWindowViewModel) {
    try {
      if (role == "delete") {
        foreach (var file in filesToRemove) {
          mainWindowViewModel.DownloadedFiles.Remove(file);
          if (File.Exists(file.FilePath)) {
            File.Delete(file.FilePath);
          }
        }
      }
      else {
        foreach (var file in filesToRemove) {
          var fileToKeep = mainWindowViewModel.DownloadedFiles[mainWindowViewModel.DownloadedFiles.IndexOf(file)];
          fileToKeep.IsEdited = false;
          fileToKeep.IsKept = true;
        }
      }

      JsonHelper.WriteDataToAppData();
      return true;
    }
    catch (Exception) {
      await MessageBoxManager.GetMessageBoxStandard("Error", "Unexpected error occurred").ShowAsync();
      return false;
    }
  }

  private async Task<bool> AttemptUpload(string filePath, MainWindowViewModel mainView, string ogIsm, string role) {
    var upload = await Upload(filePath, mainView, ogIsm);
    if (!upload) return false;

    var file = mainView.DownloadedFiles.FirstOrDefault(f => f.FilePath == filePath);
    if (file == null) return false;

    if (role == "delete") {
      return DeleteFile(file, mainView);
    } else {
      return KeepFile(file, mainView);
    }
  }

  public async Task<bool> Upload(string filePath, MainWindowViewModel mainView, string ogIsm = "") {
    try {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
        mainView.Status = FeedbackHelper.FileAccessError;
        await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel?.Status!, mainView);
        return false;
      }

      byte[] fileContentBytes = await File.ReadAllBytesAsync(filePath);
      var content = new MultipartFormDataContent {
                { new ByteArrayContent(fileContentBytes), "file", Path.GetFileName(filePath) }
            };

      var fileName = !string.IsNullOrEmpty(ogIsm) ? ogIsm : new FileInfo(filePath).Name;
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
    catch (Exception) {
      mainView.Status = FeedbackHelper.UploadFail;
      await FeedbackHelper.ShowNotificationAsync(WindowHelper.MainWindowViewModel?.Status!, mainView);
      return false;
    }
  }
}
