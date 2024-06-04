using System;
using System.Net.Http;
using System.Threading.Tasks;
using urlhandler.Helpers;
using urlhandler.ViewModels;

namespace urlhandler.Services.Concrete;
internal interface ITokenService {
  Task FetchAuthToken(MainWindowViewModel mainWindowView);
}
internal class TokenService : ITokenService {
  public async Task FetchAuthToken(MainWindowViewModel mainWindowView) {
    try {
      var response = await mainWindowView._httpClient.GetAsync(ApiHelper.TokenUrl());

      if (response.IsSuccessStatusCode) {
        var content = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrEmpty(content)) {
          mainWindowView.AuthToken = content;
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
