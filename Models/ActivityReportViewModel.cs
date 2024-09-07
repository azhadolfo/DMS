namespace Document_Management.Models
{
    public class ActivityReportViewModel
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }

        public IEnumerable<FileUploadReportViewModel> UploadedFiles { get; set; }

        public string CurrentUser { get; set; } = string.Empty;
    }

    public class FileUploadReportViewModel
    {
        public string Company { get; set; }
        public string Year { get; set; }
        public string Department { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Username { get; set; }
        public int FileCount { get; set; }
    }
}
