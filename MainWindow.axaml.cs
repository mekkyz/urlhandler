using Avalonia.Controls;
using System;
using System.IO;
using System.Linq;
using System.Timers;
using urlhandler.Helpers;
using urlhandler.Models;

namespace urlhandler {
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
      System.Timers.Timer timer = new System.Timers.Timer(3000);
      // hook up the Elapsed event for the timer. 
      timer.Elapsed += OnTimedEvent;
      timer.AutoReset = true;
      timer.Enabled = true;
    }

    private static void OnTimedEvent(Object source, ElapsedEventArgs e) {
      var downloadedFiles = WindowHelper.MainWindowViewModel.DownloadedFiles;

      for (int i = 0; i < downloadedFiles.Count; i++) {
        var lastWrite = File.GetLastWriteTime(downloadedFiles[i].FilePath);
        var creationTime = File.GetCreationTime(downloadedFiles[i].FilePath);

        if (lastWrite.Second != creationTime.Second) {
          if (WindowHelper.MainWindowViewModel.EditedFiles.Count == 0) {
            WindowHelper.MainWindowViewModel.EditedFiles.Add(new EditedFiles() {
              FileName = downloadedFiles[i].FileName,
              FilePath = downloadedFiles[i].FilePath,
              FileTime = lastWrite,
              LastEdit = lastWrite,
              IsUpdated = true
            });
          }
          else if (WindowHelper.MainWindowViewModel.EditedFiles.Where(x => x.FilePath == downloadedFiles[i].FilePath).FirstOrDefault().LastEdit.Second != lastWrite.Second) {
            WindowHelper.MainWindowViewModel.EditedFiles.Add(new EditedFiles() {
              FileName = downloadedFiles[i].FileName,
              FilePath = downloadedFiles[i].FilePath,
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
