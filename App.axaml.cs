using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using urlhandler.Helpers;
using urlhandler.Views;
using urlhandler.ViewModels;

namespace urlhandler;

public class App : Application {
  public override void Initialize() {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted() {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
      var mw = new MainWindow();
      WindowHelper.MainWindowViewModel = new MainWindowViewModel(mw, desktop.Args ?? []);
      mw.DataContext = WindowHelper.MainWindowViewModel;
      desktop.Startup += DesktopOnStartup;
      desktop.MainWindow = mw;
      desktop.MainWindow.DataContext = mw.DataContext;
      WindowHelper.MainWindow = mw;
    }

    base.OnFrameworkInitializationCompleted();
  }

  private void DesktopOnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e) {
    var currentProcess = Process.GetCurrentProcess();
    if (Process.GetProcessesByName("URL-Handler").Length > 0) {
      var processes = Process.GetProcessesByName("URL-Handler");
      foreach (Process process in processes) {
        if (process.Id != currentProcess.Id) {
          try {
            process.Kill();
          }

          catch (Exception) {
            // ignored
          }
        }
      }
    }
  }
}
