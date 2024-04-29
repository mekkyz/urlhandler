using System;
using urlhandler.ViewModels;

namespace urlhandler.Services.Abstract {
  internal interface IUpdateService {
    void DebounceUpdate(Action updateAction, MainWindowViewModel mainWindowView, int interval = 300);
    void UpdateProgress(ProgressInfo progress, string fileId, MainWindowViewModel mainWindowView);
  }
}
