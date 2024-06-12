using System;
using DesktopNotifications;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using urlhandler.ViewModels;
using System.Collections.Generic;

namespace urlhandler.Helpers;

public static class FeedbackHelper {
  public static string AlreadyExists = "File already exists!";
  public static string DownloadFail = "Failed to download file";
  public static string DownloadSuccessful = "File downloaded successfully!";
  public static string Downloading = "Downloading File...";
  public static string FileAccessError = "File access error!";
  public static string FileNotEdited = "Not edited yet!";
  public static string InvalidUrl = "Invalid URL format!";
  public static string Minimize = "Auto-minimize!";
  public static string NetworkError = "Network error!";
  public static string NoDownloads = "No downloaded files";
  public static string TokenFail = "Failed to extract token!";
  public static string UnExpectedError = "Unexpected error!";
  public static string UploadFail = "Failed to upload file(s)";
  public static string UploadSuccessful = "File(s) uploaded successfully";

  internal static async Task ShowNotificationAsync(string title, MainWindowViewModel mainWindowView) {
    try {
      if ((Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 10)
          || Environment.OSVersion.Platform == PlatformID.Unix) {

        var bodyMessages = new Dictionary<string, string>
        {
                { AlreadyExists, "Same file or same file name already exists." },
                { DownloadFail, "URL is false or expired. Try again with a different one." },
                { DownloadSuccessful, "Heroic action! Please continue like that!" },
                { UploadSuccessful, "Heroic action! Please continue like that!" },
                { FileAccessError, "Make sure the file exists and you have sufficient permissions." },
                { FileNotEdited, "File(s) not edited yet." },
                { InvalidUrl, "Ensure the URL matches the expected pattern." },
                { Minimize, "Minimized due to inactivity." },
                { NetworkError, "Please check your connection or contact support if the problem persists." },
                { NoDownloads, "There are downloaded files at the moment." },
                { UploadFail, "URL is expired. You have to download it again using a new link." },
                { UnExpectedError, "Unexpected behavior, please report." }
            };

        string body = bodyMessages.ContainsKey(title) ? bodyMessages[title] : "Unexpected behavior, please report.";

        var nf = new Notification {
          Title = title,
          Body = body,
          BodyImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico")
        };

        await mainWindowView.notificationManager!.ShowNotification(nf);
      }
    }
    catch (Exception ex) {
      Debug.WriteLine($"Error showing notification: {ex.Message}");
    }
  }
}
