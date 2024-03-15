using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document_Management.Models
{
    public class FileDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "File")]
        public string? Name { get; set; }

        [Display(Name = "File Name")]
        public string? OriginalFilename { get; set; }

        [Display(Name = "File Location")]
        public string? Location { get; set; }

        [Required]
        public string? Company { get; set; }

        public string? Year { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Date Uploaded")]
        public DateTime DateUploaded { get; set; }

        public string? Username { get; set; }

        [Required]
        public string? Category { get; set; }

        [Display(Name = "Sub Category")]
        public string SubCategory { get; set; } = "N/A";

        [Display(Name = "Number Of Pages")]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value bigger than 0")]
        public int NumberOfPages { get; set; }

        public long FileSize { get; set; }
    }
}