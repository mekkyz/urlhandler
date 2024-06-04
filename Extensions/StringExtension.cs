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
}
