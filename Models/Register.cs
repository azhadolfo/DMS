using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document_Management.Models
{
    public class Register
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Employee Number is required.")]
        [Display(Name = "Employee Number")]
        [Range(1, int.MaxValue, ErrorMessage = "Employee Number must be a positive integer.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Employee Number must be a 4-digit number.")]
        public int EmployeeNumber { get; set; }

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "First Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in First Name.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "Last Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in Last Name.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        //[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")] //Uncomment this after the complete production
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = null!;

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; } = null!;

        public string AccessFolders { get; set; } = null!;
    }
}