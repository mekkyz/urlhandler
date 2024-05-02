using System.Diagnostics;
using System;
using urlhandler.Models;
using urlhandler.Services.Abstract;
using urlhandler.ViewModels;
using System.IO;
using urlhandler.Helpers;

namespace urlhandler.Services.Concrete {
  internal class FileService : IFileService {
    public void ProcessFile(string? filePath, MainWindowViewModel mainWindowView) {
      try {
        if (filePath != null) {
          WindowHelper.MainWindowViewModel._fileProcess?.Dispose();

          WindowHelper.MainWindowViewModel._fileProcess = new Process {
            StartInfo = new ProcessStartInfo(filePath) {
              UseShellExecute = true
            }
          };
          WindowHelper.MainWindowViewModel._fileProcess.Start();

          Downloads download = new Downloads() {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            FileTime = File.GetLastWriteTime(filePath),
          };
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
}
