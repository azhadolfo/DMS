namespace Document_Management.Models
{
    public class ActivityReportViewModel
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }

        public IEnumerable<FileUploadReportViewModel>? UploadedFiles { get; set; }

        public string CurrentUser { get; set; } = string.Empty;
    }

    public class FileUploadReportViewModel
    {
        public string Company { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public string BoxNumber { get; set; } = string.Empty;
        public string SubmittedBy { get; set; } = string.Empty;
        public DateOnly? DateSubmitted { get; set; }
    }
}
