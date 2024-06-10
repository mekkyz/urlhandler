using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using urlhandler.Models;

namespace urlhandler.Extensions;

public static class HttpClientExtension {
  private const int BufferSize = 8192;

  public static async Task<(HttpResponseMessage Response, byte[] Content)> GetWithProgressAsync(this HttpClient client, string requestUri, IProgress<ProgressInfo> progress, CancellationToken cancellationToken = default) {
    using var responseMessage = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    if (!responseMessage.IsSuccessStatusCode) {
      return (responseMessage, []);
    }

    var content = await ProcessResponseAsync(responseMessage, progress, cancellationToken);
    return (responseMessage, content);
  }

  public static async Task<HttpResponseMessage> PostWithProgressAsync(this HttpClient client, string requestUri, HttpContent content, IProgress<ProgressInfo> progress, CancellationToken cancellationToken = default, bool isUpload = false) {
    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
    requestMessage.Content = content;

    var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    if (isUpload) await UploadWithProgressAsync(content, progress, cancellationToken);
    else await ProcessResponseAsync(responseMessage, progress, cancellationToken);
    return responseMessage;
  }
  private static async Task UploadWithProgressAsync(HttpContent content, IProgress<ProgressInfo> progress, CancellationToken cancellationToken) {
    await using var contentStream = await content.ReadAsStreamAsync(cancellationToken);

    var totalBytesExpected = content.Headers.ContentLength ?? -1;
    var totalBytesRead = 0L;
    var totalReportedRead = 0L;
    var buffer = new byte[BufferSize];

    while (true) {
      var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
      if (bytesRead == 0)
        break;

      totalBytesRead += bytesRead;

      if (totalBytesRead - totalReportedRead >= BufferSize) {
        var percentage = totalBytesExpected > 0 ? (double)totalBytesRead / totalBytesExpected * 100 : -1;
        progress.Report(new ProgressInfo(totalBytesRead, totalBytesExpected, percentage));
        totalReportedRead = totalBytesRead;
      }
    }

    var finalPercentage = totalBytesExpected > 0 ? (double)totalBytesRead / totalBytesExpected * 100 : -1;
    progress.Report(new ProgressInfo(totalBytesRead, totalBytesExpected, finalPercentage));
  }

  private static async Task<byte[]> ProcessResponseAsync(HttpResponseMessage responseMessage, IProgress<ProgressInfo> progress, CancellationToken cancellationToken) {
    await using var contentStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);

    var totalBytesExpected = responseMessage.Content.Headers.ContentLength ?? -1;
    var totalBytesRead = 0L;
    var totalReportedRead = 0L;
    var buffer = new byte[BufferSize];
    var contentBytes = new MemoryStream();

    while (true) {
      var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
      if (bytesRead == 0)
        break;

      totalBytesRead += bytesRead;
      await contentBytes.WriteAsync(buffer, 0, bytesRead, cancellationToken);

      if (totalBytesRead - totalReportedRead >= BufferSize) {
        var percentage = totalBytesExpected > 0 ? (double)totalBytesRead / totalBytesExpected * 100 : -1;
        progress.Report(new ProgressInfo(totalBytesRead, totalBytesExpected, percentage));
        totalReportedRead = totalBytesRead;
      }
    }

    var finalPercentage = totalBytesExpected > 0 ? (double)totalBytesRead / totalBytesExpected * 100 : -1;
    progress.Report(new ProgressInfo(totalBytesRead, totalBytesExpected, finalPercentage));

    return contentBytes.ToArray();
  }
}
