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
        public string Company { get; set; }

        public string Year { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Date Uploaded")]
        public DateTime DateUploaded { get; set; }

        public string Username { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; }

        [Display(Name = "Sub Category")]
        public string SubCategory { get; set; } = "N/A";

        [Display(Name = "Number Of Pages")]
        public int NumberOfPages { get; set; }

        public long FileSize { get; set; }
        
        // Add this property to track storage location
        public bool IsInCloudStorage { get; set; }


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