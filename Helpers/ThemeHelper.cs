using System;
using System.IO;

public static class Theme {
  private static string settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "URL Handler", "theme.txt");

  public static void SaveCurrentTheme(bool isDarkMode) {
    var directory = Path.GetDirectoryName(settingsFilePath);
    if (!Directory.Exists(directory)) {
      Directory.CreateDirectory(directory!);
    }
    File.WriteAllText(settingsFilePath, isDarkMode ? "Dark" : "Light");
  }

  public static bool LoadCurrentTheme() {
    if (File.Exists(settingsFilePath)) {
      var theme = File.ReadAllText(settingsFilePath);
      return theme == "Dark";
    }
    return false;
  }
}
