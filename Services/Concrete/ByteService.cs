using urlhandler.Services.Abstract;

namespace urlhandler.Services.Concrete {
  internal class ByteService : IByteService {
    const int scale = 1024;

    public string FormatBytes(long bytes) {
      string[] orders = new string[] { "B", "KB", "MB", "GB", "TB" };
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
}
