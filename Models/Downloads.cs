using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace urlhandler.Models;

public partial class Downloads : ObservableObject {
  // FileId to keep track of edited files in a list
  public float FileId { get; set; }
  public string FileName { get; set; } = string.Empty;
  public string OriginalFileName { get; set; } = string.Empty;
  public string FilePath { get; set; } = string.Empty;
  public DateTime FileDownloadTimeStamp { get; set; }
  [ObservableProperty] private string? _fileSize;
  public string? FileSumOnDownload { get; set; }

  [ObservableProperty] private bool _isEdited;
  [ObservableProperty] private bool _isKept;
  [ObservableProperty] private long _exp;
}
