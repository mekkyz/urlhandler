using System;
using System.IO;
using System.Security.Cryptography;
using System.Web;

namespace urlhandler.Extensions;

public static class StringExtension {
  private const int scale = 1024;
  private static readonly string[] orders = { "B", "KB", "MB", "GB", "TB" };

  public static string FormatBytes(this long bytes) {
    int orderIndex = 0;
    decimal adjustedBytes = bytes;

    while (adjustedBytes >= scale && orderIndex < orders.Length - 1) {
      adjustedBytes /= scale;
      orderIndex++;
    }

    // format the bytes with the appropriate unit
    return $"{adjustedBytes:##.##} {orders[orderIndex]}";
  }
  public static string FileCheckSum(this string filePath) {
    using var stream = File.OpenRead(filePath);
    using var sha256 = SHA256.Create();
    var checksum = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLower();
    return checksum;
  }

  public static string? ParseUrl(this string inputUrl) {
    try {
      var uri = new Uri(inputUrl);
      var parse = HttpUtility.ParseQueryString(uri.Query).Get("url");

      return HttpUtility.UrlDecode(parse);
    }
    catch (Exception) {
      return "invalid uri";
    }
  }

  public static string? ExtractAuthToken(this string decodedUrl) {
    var lastSlashIndex = decodedUrl.LastIndexOf('/');
    if (lastSlashIndex < 0 || lastSlashIndex == decodedUrl.Length - 1) {
      return null;
    }
    return decodedUrl[(lastSlashIndex + 1)..];
  }
}
