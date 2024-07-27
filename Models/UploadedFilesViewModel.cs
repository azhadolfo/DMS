namespace Document_Management.Models
{
    public class UploadedFilesViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string LocationFolder { get; set; }

        public string UploadedBy { get; set; }

        public DateTime DateUploaded { get; set; }
    }
}