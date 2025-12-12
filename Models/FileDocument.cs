using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Document_Management.Models
{
    public class FileDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Display(Name = "File")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "File Name")]
        public string OriginalFilename { get; set; } = string.Empty;

        [Display(Name = "File Location")]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Company { get; set; }  = string.Empty;

        public string Year { get; set; } =  string.Empty;

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Date Uploaded")]
        public DateTime DateUploaded { get; set; }

        public string Username { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Sub Category")]
        public string SubCategory { get; set; } = "N/A";

        [Display(Name = "Number Of Pages")]
        public int NumberOfPages { get; set; }

        public long FileSize { get; set; }
        
        public bool IsInCloudStorage { get; set; }

        public bool IsDeleted { get; set; }

        [Display(Name = "Box Number")]
        public string BoxNumber { get; set; } = string.Empty;

        [Display(Name = "Submitted By")]
        public string SubmittedBy { get; set; } = string.Empty;

        [Display(Name = "Date Submitted")]
        public DateOnly DateSubmitted { get; set; }

        #region Select List Properties

        [NotMapped]
        public List<SelectListItem>? Companies { get; set; }
        
        [NotMapped]
        public List<SelectListItem>? Years { get; set; }
        
        [NotMapped]
        public List<SelectListItem>? Departments { get; set; }
        
        [NotMapped]
        public List<SelectListItem>? Categories { get; set; }
        
        [NotMapped]
        public List<SelectListItem>? SubCategories { get; set; }

        #endregion
    }
}