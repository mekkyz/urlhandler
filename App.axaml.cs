using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using urlhandler.ViewModels;

namespace urlhandler;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mw = new MainWindow();
            mw.DataContext = new MainWindowViewModel(mw, desktop.Args);
            desktop.MainWindow = mw;
            desktop.MainWindow.DataContext = mw.DataContext;
        }

        base.OnFrameworkInitializationCompleted();
    }
}