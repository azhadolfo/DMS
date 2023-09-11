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
        [Required]
        public string? Name { get; set; }
        [Display(Name = "File Location")]
        public string? Location { get; set; }
        [Required]
        public string? Department { get; set; } 
        [Required]
        public string? Description { get; set; } 
        [Display(Name = "Date Uploaded")]
        public DateTime DateUploaded { get; set; }
        public string? Username { get; set; } 
    }
}
