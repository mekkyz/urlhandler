using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using urlhandler.ViewModels;

namespace urlhandler.Behaviors;

public class UrlBoxBehavior : Behavior<TextBox> {
  protected override void OnAttached() {
    base.OnAttached();
    AssociatedObject!.KeyUp += AssociatedObjectOnKeyUp;
  }

  private void AssociatedObjectOnKeyUp(object? sender, KeyEventArgs e) {
    if (Application.Current!.ApplicationLifetime is not ClassicDesktopStyleApplicationLifetime app) return;
    if (e.Key != Key.Enter || string.IsNullOrEmpty((sender as TextBox)?.Text)) return;
    var vm = app.MainWindow!.DataContext as MainWindowViewModel;
    Task.Run(async () =>
      await vm!.ProcessCommand()
    );
  }

  protected override void OnDetaching() {
    base.OnDetaching();
    if (AssociatedObject != null) AssociatedObject.KeyUp -= AssociatedObjectOnKeyUp;
  }
}
