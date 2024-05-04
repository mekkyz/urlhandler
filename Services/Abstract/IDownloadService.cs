using System.Collections.Generic;
using System.Threading.Tasks;
using urlhandler.ViewModels;

namespace urlhandler.Services.Abstract {
  internal interface IDownloadService {
    Task<string?> DownloadFile(string? fileId, int authtoken, MainWindowViewModel mainWindowView);
    Task DownloadFilesConcurrently(IEnumerable<string> fileIds, int authtoken, MainWindowViewModel mainWindowView);
  }
}
