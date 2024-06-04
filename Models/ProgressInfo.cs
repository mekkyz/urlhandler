namespace urlhandler.Models;

public class ProgressInfo {
  public long BytesRead { get; }
  public long? TotalBytesExpected { get; }
  public double Percentage { get; }

  public ProgressInfo(long bytesRead, long? totalBytesExpected, double percentage) {
    BytesRead = bytesRead;
    TotalBytesExpected = totalBytesExpected;
    Percentage = percentage;
  }
}
