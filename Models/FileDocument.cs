using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document_Management.Models
{
    public class FileDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [Display(Name = "File Name")]
        public string Name { get; set; } = null!;
        [Required]
        [Display(Name = "File Location")]
        public string Location { get; set; } = null!;
        [Required]
        public string Department { get; set; } = null!;
        [Required]
        public string Description { get; set; } = null!;
        [Required]
        [Display(Name = "Date Uploaded")]
        public DateTime DateUploaded { get; set; }
        [Required]
        public string Username { get; set; } = null!;
    }
}
