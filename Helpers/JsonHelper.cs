using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace urlhandler.Helpers;

public static class JsonHelper {
  public static void AppendJsonToFile(string path, object jsonObject) {
    string json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

    if (File.Exists(path) && !string.IsNullOrEmpty(File.ReadAllText(path))) {
      string existingJson = File.ReadAllText(path);

      var existingEntries = JsonConvert.DeserializeObject<List<object>>(existingJson);

      existingEntries.Add(jsonObject);

      json = JsonConvert.SerializeObject(existingEntries, Formatting.Indented);
    }
    else {
      var newEntries = new List<object> { jsonObject };

      json = JsonConvert.SerializeObject(newEntries, Formatting.Indented);
    }

    File.WriteAllText(path, json);
  }

  public static async void WriteDataToAppData() {
    var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "URL Handler");
    Directory.CreateDirectory(appDataPath);
    var jsonFilePath = Path.Combine(appDataPath, "downloads.json");
    var data = JsonConvert.SerializeObject(WindowHelper.MainWindowViewModel!.DownloadedFiles);
    await File.WriteAllTextAsync(jsonFilePath, data);
  }
}
