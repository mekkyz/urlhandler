using System;
using System.IO;
using System.Linq;
using urlhandler.ViewModels;

namespace urlhandler.Services;

internal interface IHistoryService {
  void LoadHistory(MainWindowViewModel mainWindowView);
  void AddHistory(MainWindowViewModel mainWindowView, string url);
  void DeleteHistory(MainWindowViewModel mainWindowView);
  void IndexChange(MainWindowViewModel mainWindowView, int val);
}

internal class HistoryService : IHistoryService {
  public void LoadHistory(MainWindowViewModel mainWindowView) {
    var historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.txt");
    if (File.Exists(historyFilePath)) {
      var lines = File.ReadAllLines(historyFilePath);
      if (lines.Length > 0) {
        var sortedItems = lines
          .OrderByDescending(item => {
            var timestampStr = item.Substring(0, item.IndexOf('|'));
            return DateTime.Parse(timestampStr);
          })
          .ToList();
        foreach (var url in sortedItems) {
          mainWindowView.HasHistory = true;
          mainWindowView.History.Add(url);
        }
      }
    }
  }

  public void AddHistory(MainWindowViewModel mainWindowView, string url) {
    try {
      // load History from .txt file if exists
      mainWindowView.History.Insert(0, $"{DateTime.Now.ToString()}|" + url);
      var historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.txt");
      if (File.Exists(historyFilePath)) {
        File.AppendAllLines(historyFilePath, new[] { $"{DateTime.Now.ToString()}|" + url });
      }
      else {
        File.WriteAllLines(historyFilePath, new[] { $"{DateTime.Now.ToString()}|" + url });
      }

      mainWindowView.HasHistory = mainWindowView.History.Any();
    }
    catch (Exception ex) {
      Console.WriteLine($"Error in AddHistory: {ex.Message}");
      throw;
    }
  }

  public void DeleteHistory(MainWindowViewModel mainWindowView) {
    try {
      var historyFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history.txt");

      if (mainWindowView.SelectedHistoryIndex > -1 && mainWindowView.SelectedHistoryIndex < mainWindowView.History.Count) {
        mainWindowView.History.RemoveAt(mainWindowView.SelectedHistoryIndex);
      }
      else {
        mainWindowView.History.Clear();
      }
      if (File.Exists(historyFilePath)) {
        if (mainWindowView.History.Count > 0) {
          File.WriteAllLines(historyFilePath, mainWindowView.History);
        }
        else {
          File.Delete(historyFilePath);
        }
      }

      mainWindowView.HasHistory = mainWindowView.History.Any();
    }
    catch (Exception ex) {
      Console.WriteLine($"Error in DeleteHistory: {ex.Message}");
      throw;
    }
  }

  public void IndexChange(MainWindowViewModel mainWindowView, int val) {
    try {
      if (mainWindowView.HasHistory) {
        if (val >= 0 && val < mainWindowView.History.Count) {
          mainWindowView.Url = mainWindowView.History[val].Substring(mainWindowView.History[val].IndexOf('|') + 1);
        }
        else {
          throw new IndexOutOfRangeException("Selected history index is out of range.");
        }
      }
    }

    catch (Exception ex) {
      Console.WriteLine($"Error in OnSelectedHistoryIndexChanged: {ex.Message}");
    }
  }
}
