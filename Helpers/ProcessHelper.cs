using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using urlhandler.Extensions;
using urlhandler.ViewModels;

namespace urlhandler.Helpers;

internal abstract class ProcessHelper {
  public static async Task HandleProcess(MainWindowViewModel mainWindowView, string _url) {
    try {
      if (mainWindowView.IsAlreadyProcessing == false) {

        if (!Uri.TryCreate(mainWindowView.Url, UriKind.Absolute, out _)) {
          mainWindowView.Status = FeedbackHelper.InvalidUrl;
          await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

          return;
        }

        if (mainWindowView.Url.ToLower().Contains("url=")) {
          var uri = new Uri(mainWindowView.Url);
          string? parm = HttpUtility.ParseQueryString(uri.Query).Get("url");
          if (!string.IsNullOrEmpty(parm)) {
            mainWindowView.Url = parm;
            _url = parm;
            if (mainWindowView.Url != _url) {
              mainWindowView.SelectedUrl = _url;
              mainWindowView.Url = _url;
            }
          }
        }

        if (!string.IsNullOrEmpty(_url) && mainWindowView.Url != _url)
          mainWindowView.Url = _url;
        var token = _url.ExtractAuthToken();
        mainWindowView._filePath = await mainWindowView._downloadService.DownloadFile(mainWindowView, token!);
        if (mainWindowView._filePath == null) {
          mainWindowView.Status = FeedbackHelper.DownloadFail;
          await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return;
        }
        if (mainWindowView._filePath == "Already Exists!") {
          mainWindowView.Status = FeedbackHelper.AlreadyExists;
          await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
          return;
        }

        await mainWindowView._fileService.ProcessFile(mainWindowView._filePath, mainWindowView);
        mainWindowView.Status = FeedbackHelper.DownloadSuccessful;
        await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);

      }
      else {
        mainWindowView.Status = FeedbackHelper.FileAccessError;
        await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
      }
    }

    catch (HttpRequestException) {
      mainWindowView.Status = FeedbackHelper.NetworkError;
      await FeedbackHelper.ShowNotificationAsync(mainWindowView.Status, mainWindowView);
    }

    catch (Exception ex) {
      Console.WriteLine($"Error in Process method: {ex.Message}");
      throw;
    }
  }
}
