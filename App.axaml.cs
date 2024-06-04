using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using urlhandler.Helpers;
using urlhandler.Views;
using urlhandler.ViewModels;

namespace urlhandler;

public partial class App : Application {
  public override void Initialize() {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted() {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
      var mw = new MainWindow();
      WindowHelper.MainWindowViewModel = new MainWindowViewModel(mw, desktop.Args ?? []);
      mw.DataContext = WindowHelper.MainWindowViewModel;
      desktop.MainWindow = mw;
      desktop.MainWindow.DataContext = mw.DataContext;
      WindowHelper.MainWindow = mw;
    }

    base.OnFrameworkInitializationCompleted();
  }
}
