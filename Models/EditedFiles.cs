using System;

namespace urlhandler.Models {
    public class EditedFiles {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime FileTime { get; set; }
        public DateTime LastEdit { get; set; }
        public bool IsUpdated { get; set; } = false;
    }
}
