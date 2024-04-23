using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public static class HttpClientExtensions {
  private const int BufferSize = 8192;

  public static async Task<(HttpResponseMessage Response, byte[] Content)> GetWithProgressAsync(this HttpClient client, string requestUri, IProgress<ProgressInfo> progress, CancellationToken cancellationToken = default) {
    using var responseMessage = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    if (!responseMessage.IsSuccessStatusCode) {
      return (responseMessage, Array.Empty<byte>());
    }

    var content = await ProcessResponseAsync(responseMessage, progress, cancellationToken);
    return (responseMessage, content);
  }

  public static async Task<HttpResponseMessage> PostWithProgressAsync(this HttpClient client, string requestUri, HttpContent content, IProgress<ProgressInfo> progress, CancellationToken cancellationToken = default) {
    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri) {
      Content = content
    };

    using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    await ProcessResponseAsync(responseMessage, progress, cancellationToken);
    return responseMessage;
  }

  private static async Task<byte[]> ProcessResponseAsync(HttpResponseMessage responseMessage, IProgress<ProgressInfo> progress, CancellationToken cancellationToken) {
    using var contentStream = await responseMessage.Content.ReadAsStreamAsync();
    var totalBytesExpected = responseMessage.Content.Headers.ContentLength ?? -1;
    var totalBytesRead = 0L;
    var totalReportedRead = 0L;
    var buffer = new byte[BufferSize];
    var bytesRead = 0;
    var contentBytes = new MemoryStream();

    do {
      bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
      totalBytesRead += bytesRead;
      contentBytes.Write(buffer, 0, bytesRead);

      if (totalBytesRead - totalReportedRead > BufferSize) {
        var _percentage = totalBytesExpected > 0 ? (double)totalBytesRead / totalBytesExpected * 100 : -1;
        progress.Report(new ProgressInfo(totalBytesRead, totalBytesExpected, _percentage));
        totalReportedRead = totalBytesRead;
      }
    }
    while (bytesRead > 0);

    var percentage = totalBytesExpected > 0 ? (double)totalBytesRead / totalBytesExpected * 100 : -1;
    progress.Report(new ProgressInfo(totalBytesRead, totalBytesExpected, percentage));

    return contentBytes.ToArray();
  }
}
