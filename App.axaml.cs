using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using urlhandler.ViewModels;
using System;
using urlhandler.Helpers;

namespace urlhandler;

public partial class App : Application {
  public override void Initialize() {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted() {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
      var mw = new MainWindow();
      string[] args = desktop.Args ?? Array.Empty<string>();
      WindowHelper.MainWindowViewModel = new MainWindowViewModel(mw, args);
      mw.DataContext = WindowHelper.MainWindowViewModel;
      desktop.MainWindow = mw;
      desktop.MainWindow.DataContext = mw.DataContext;
      WindowHelper.MainWindow = mw;
    }

    base.OnFrameworkInitializationCompleted();
  }
}
