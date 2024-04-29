using System.Diagnostics;
using System;
using urlhandler.Models;
using urlhandler.Services.Abstract;
using urlhandler.ViewModels;
using System.IO;

namespace urlhandler.Services.Concrete {
  internal class FileService : IFileService {
    public void ProcessFile(string? filePath, MainWindowViewModel mainWindowView) {
      try {
        if (filePath != null) {
          mainWindowView._fileProcess?.Dispose();

          mainWindowView._fileProcess = new Process {
            StartInfo = new ProcessStartInfo(filePath) {
              UseShellExecute = true
            }
          };
          mainWindowView._fileProcess.Start();

          mainWindowView.DownloadedFiles.Add(new Downloads() {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            FileTime = File.GetLastWriteTime(filePath)
          });
          mainWindowView.HasFilesDownloaded = mainWindowView.DownloadedFiles.Count > 0;
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error processing file: {ex.Message}");
      }
    }
  }
}
