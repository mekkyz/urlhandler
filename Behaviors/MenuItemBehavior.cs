using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using CommunityToolkit.Mvvm.Input;
using urlhandler.ViewModels;

namespace urlhandler.Behaviors;

public class MenuItemBehavior : Behavior<MenuItem> {
  public static readonly StyledProperty<object> ViewModelProperty =
    AvaloniaProperty.Register<MenuItemBehavior, object>(nameof(ViewModel));

  public object ViewModel {
    get => GetValue(ViewModelProperty);
    set => SetValue(ViewModelProperty, value);
  }
  public static readonly StyledProperty<object> CommandsProperty =
    AvaloniaProperty.Register<MenuItemBehavior, object>(nameof(IRelayCommand));

  public object Commands {
    get => GetValue(CommandsProperty);
    set => SetValue(CommandsProperty, value);
  }


  protected override void OnAttached() {
    base.OnAttached();
    if (AssociatedObject != null) {
      AssociatedObject.Click += OnMenuItemClick!;
    }
  }

  protected override void OnDetaching() {
    base.OnDetaching();
    if (AssociatedObject != null) {
      AssociatedObject.Click -= OnMenuItemClick!;
    }
  }

  private async void OnMenuItemClick(object sender, Avalonia.Interactivity.RoutedEventArgs e) {
    if (ViewModel != null) {
      if (Commands != null) {

        var viewModel = ViewModel as MainWindowViewModel;
        switch (Commands as string) {

          case "uploadndelete":
            await viewModel!.UploadFiles("delete");
            break;
          case "uploadnkeep":
            await viewModel!.UploadFiles("");
            break;
          case "deleteFile":
            viewModel?.DeleteSelectedFile();
            break;
          case "openFile":
            viewModel?.OpenFile();
            break;
          case "openDir":
            viewModel?.OpenDownloadDirectory();
            break;
          default:
            break;
        }
      }
    }
  }
}
