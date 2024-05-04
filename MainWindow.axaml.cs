using Avalonia.Controls;
using System;
using System.IO;
using System.Linq;
using System.Timers;
using urlhandler.Helpers;
using urlhandler.Models;

namespace urlhandler {
  public partial class MainWindow : Window {
    private static DateTime _openingTime;
    public MainWindow() {
      InitializeComponent();
      Timer timer = new Timer(3000);
      // hook up elapsed event for the timer. 
      timer.Elapsed += OnTimedEvent;
      timer.AutoReset = true;
      timer.Enabled = true;
      _openingTime = DateTime.Now;
    }

    private static void OnTimedEvent(object? source, ElapsedEventArgs e) {
      var downloadedFiles = WindowHelper.MainWindowViewModel?.DownloadedFiles;
      if (downloadedFiles != null) {
        for (int i = 0; i < downloadedFiles.Count; i++) {
          var file = downloadedFiles[i];
          if (file?.FilePath != null) {
            var lastWrite = File.GetLastWriteTime(file.FilePath);
            var creationTime = File.GetCreationTime(file.FilePath);
            var temp = WindowHelper.MainWindowViewModel?.EditedFiles.FirstOrDefault(x => x.FilePath == file.FilePath);

            if (lastWrite >= creationTime && lastWrite.Second != creationTime.Second) {
              if (temp != null) {
                temp.LastEdit = lastWrite;
              }
              else {
                WindowHelper.MainWindowViewModel?.EditedFiles.Add(new EditedFiles {
                  FileName = file.FileName,
                  FilePath = file.FilePath,
                  FileTime = lastWrite,
                  LastEdit = lastWrite,
                  IsUpdated = true
                });
              }
            }
          }
        }
      }
    }
  }
}
