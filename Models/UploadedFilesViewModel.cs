namespace Document_Management.Models
{
    public class UploadedFilesViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public required string Description { get; set; }

        public string LocationFolder { get; set; } = string.Empty;

        public string UploadedBy { get; set; } = string.Empty;

        public DateTime DateUploaded { get; set; }

        public string BoxNumber { get; set; } = string.Empty;

        public string SubmittedBy { get; set; } = string.Empty;

        public DateOnly DateSubmitted { get; set; }
    }
}