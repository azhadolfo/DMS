using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document_Management.Models
{
    public class FileDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "File Name")]
        public string? Name { get; set; }

        [Display(Name = "File Location")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(255, ErrorMessage = "Description must be less than 255 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Date Uploaded")]
        public DateTime DateUploaded { get; set; }

        public string? Username { get; set; }
    }
}