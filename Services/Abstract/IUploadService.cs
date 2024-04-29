using System.Threading.Tasks;
using urlhandler.ViewModels;

namespace urlhandler.Services.Abstract {
  internal interface IUploadService {
    Task<bool> UploadFiles(MainWindowViewModel mainWindowView, bool ignoreIndex = false);
    Task<bool> UploadFiles(MainWindowViewModel mainWindowView);
  }
}
