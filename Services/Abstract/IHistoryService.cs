using urlhandler.ViewModels;

namespace urlhandler.Services.Abstract {
  internal interface IHistoryService {
    void LoadHistory(MainWindowViewModel mainWindowView);
    void AddHistory(MainWindowViewModel mainWindowView, string url);
    void DeleteHistory(MainWindowViewModel mainWindowView);
    void IndexChange(MainWindowViewModel mainWindowView, int val);
  }
}
