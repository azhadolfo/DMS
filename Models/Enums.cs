namespace Document_Management.Models
{
    public enum Roles
    {
        Admin,
        User,
        Validator,
        Uploader,
    }

    public static class OcrStatuses
    {
        public const string NotRequested = "NotRequested";
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
