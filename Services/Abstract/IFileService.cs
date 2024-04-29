using urlhandler.ViewModels;

namespace urlhandler.Services.Abstract {
  internal interface IFileService {
    void ProcessFile(string? filePath, MainWindowViewModel mainWindowView);
  }
}
