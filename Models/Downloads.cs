using System;

namespace urlhandler.Models;

public class Downloads {
  public string FileName { get; set; } = string.Empty;
  public string FilePath { get; set; } = string.Empty;
  public DateTime FileTime { get; set; }

}
