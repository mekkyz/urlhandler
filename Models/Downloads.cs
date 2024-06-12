using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace urlhandler.Models;

public partial class Downloads : ObservableObject {
  // FileId to keep track of edited files in a list
  public float FileId { get; set; }
  public string FileName { get; set; } = string.Empty;
  public string FilePath { get; set; } = string.Empty;
  public DateTime FileDownloadTimeStamp { get; set; }
  private string? _fileSize;

  public string FileSize {
    get => _fileSize!;
    set => SetProperty(ref _fileSize, value);
  }
  public string? FileSumOnDownload { get; set; }

  private bool _isEdited;

  public bool IsEdited {
    get => _isEdited;
    set => SetProperty(ref _isEdited, value);
  }
}
