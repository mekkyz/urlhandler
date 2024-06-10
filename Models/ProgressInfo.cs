namespace urlhandler.Models;

public class ProgressInfo(long bytesRead, long? totalBytesExpected, double percentage) {
  public long BytesRead { get; } = bytesRead;
  public long? TotalBytesExpected { get; } = totalBytesExpected;
  public double Percentage { get; } = percentage;
}
