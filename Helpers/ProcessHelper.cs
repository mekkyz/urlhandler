using System;
using System.Net.Http;
using System.Threading.Tasks;
using MsBox.Avalonia;
using urlhandler.ViewModels;

namespace urlhandler.Helpers;

internal abstract class ProcessHelper {
  public static async Task HandleProcess(MainWindowViewModel mainWindowView) {
    try {
      if (mainWindowView.IsAlreadyProcessing == false) {

        if (!Uri.TryCreate(mainWindowView.Url, UriKind.Absolute, out _)) {
          mainWindowView.Status = "Invalid URL format.";
          await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

          return;
        }

        mainWindowView.AddHistory(mainWindowView.Url);
        mainWindowView._filePath = await mainWindowView._downloadService.DownloadFile(mainWindowView);
        if (mainWindowView._filePath == null) {
          mainWindowView.Status = "Failed to download file.";
          await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return;
        }
        await mainWindowView._fileService.ProcessFile(mainWindowView._filePath, mainWindowView);

        await Task.Delay(1000);
        mainWindowView.Status = "File downloaded successfully.";
        await NotificationHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

      }
      else {
        await MessageBoxManager
            .GetMessageBoxStandard(
                "Error",
                "Another file is already being processed. Please wait for it to complete before running another."
            )
            .ShowAsync();
      }
    }

    catch (HttpRequestException ex) {
      var detailedError = $"Network error occurred: {ex.Message}. Please check your connection or contact support if the problem persists.";
      await MessageBoxManager.GetMessageBoxStandard("Error", detailedError).ShowAsync();
    }

    catch (Exception ex) {
      Console.WriteLine($"Error in Process method: {ex.Message}");
      throw;
    }
  }
}
