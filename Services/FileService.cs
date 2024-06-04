using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using urlhandler.Models;
using urlhandler.ViewModels;
using urlhandler.Helpers;

namespace urlhandler.Services.Concrete;

internal interface IFileService {
  void ProcessFile(string? filePath, MainWindowViewModel mainWindowView);
}

internal class FileService : IFileService {
  public void ProcessFile(string? filePath, MainWindowViewModel mainWindowView) {
    try {
      if (filePath != null) {
        WindowHelper.MainWindowViewModel?._fileProcess?.Dispose();

        if (WindowHelper.MainWindowViewModel != null) {
          WindowHelper.MainWindowViewModel._fileProcess = new Process {
            StartInfo = new ProcessStartInfo(filePath) {
              UseShellExecute = true
            }
          };
        }

        else {
          throw new InvalidOperationException("MainWindowViewModel is not initialized.");
        }

        WindowHelper.MainWindowViewModel._fileProcess.Start();

        Downloads download = new Downloads() {
          FileName = Path.GetFileName(filePath),
          FilePath = filePath,
          FileTime = File.GetLastWriteTime(filePath),
        };

        if (WindowHelper.MainWindowViewModel.DownloadedFiles.Where(x => x.FilePath == filePath).FirstOrDefault() == null) {
          File.SetCreationTime(filePath, DateTime.Now);

        }

        var lastWrite = File.GetLastWriteTime(filePath);
        var creationTime = File.GetCreationTime(filePath);

        WindowHelper.MainWindowViewModel.DownloadedFiles.Add(download);

        if (lastWrite.Second != creationTime.Second) {
          download.IsUpdated = true;
        }
        WindowHelper.MainWindowViewModel.HasFilesDownloaded = mainWindowView.DownloadedFiles.Count > 0;
      }
    }
    catch (Exception ex) {
      Console.WriteLine($"Error processing file: {ex.Message}");
    }
  }
}
