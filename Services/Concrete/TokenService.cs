using System;
using System.Net.Http;
using System.Threading.Tasks;
using urlhandler.Services.Abstract;
using urlhandler.ViewModels;

namespace urlhandler.Services.Concrete {
  internal class TokenService : ITokenService {
    public async Task FetchAuthToken(MainWindowViewModel mainWindowView) {
      try {
        var response = await mainWindowView._httpClient.GetAsync("http://localhost:3000/get_token");
        if (response.IsSuccessStatusCode) {
          var content = await response.Content.ReadAsStringAsync();
          if (int.TryParse(content, out int token)) {
            mainWindowView.AuthToken = token;
          }
          else {
            // handle invalid token format
            throw new InvalidOperationException("Failed to parse auth token from response content.");
          }
        }
        else {
          // handle unsuccessful HTTP response
          throw new HttpRequestException($"Failed to fetch auth token. Status code: {response.StatusCode}");
        }
      }
      catch (HttpRequestException ex) {
        Console.WriteLine($"Error fetching auth token: {ex.Message}");
        throw;
      }
      catch (Exception ex) {
        Console.WriteLine($"Error fetching auth token: {ex.Message}");
        throw;
      }
    }
  }
}
