using System.ComponentModel.DataAnnotations;

namespace DocumentManagement.Models
{
    public class Employee
    {
        [Required]
        public int Id { get; set; }
        [Required]
        [Display(Name = "Employee Number")]
        [MinLength(4, ErrorMessage = "Employee Number must atleast {1} character")]
        public int EmployeeNumber { get; set; }
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;
        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;
    }
}