using System.Threading.Tasks;
using urlhandler.ViewModels;

namespace urlhandler.Services.Abstract {
  internal interface ITokenService {
    Task FetchAuthToken(MainWindowViewModel mainWindowView);
  }
}
