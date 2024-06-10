using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace urlhandler.Models;

public partial class Downloads : ObservableObject {
  // FileId to keep track of edited files in a list
  public float FileId { get; init; }
  public string FileName { get; set; } = string.Empty;
  public string FilePath { get; set; } = string.Empty;
  public DateTime FileDownloadTimeStamp { get; set; }
  [ObservableProperty] private string? _fileSize;
  public string FileSumOnDownload { get; init; } = "";

  [ObservableProperty] private bool _isEdited;
}
