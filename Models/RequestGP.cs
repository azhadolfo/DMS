using System.ComponentModel.DataAnnotations;
using System.Configuration;

namespace Document_Management.Models
{
    public class RequestGP
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "Last Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in Last Name.")]
        [Display(Name = "Last Name", Prompt = "Enter your last name")]
        public string LastName { get; set; } = null!;

        [Required]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "First Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in First Name.")]
        [Display(Name = "First Name", Prompt = "Enter your first name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "Last Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in Middle Name.")]
        [Display(Name = "Middle Name", Prompt = "Enter your middle name")]
        public string MiddleName { get; set; } = null!;

        [Required]
        [RegularExpression(@"^(9)\d{9}$", ErrorMessage = "Contact Number must be a valid Philippine phone number. +63")]
        [Display(Prompt = "Contact number +63")]
        public long Contact { get; set; }

        [Display(Name = "Remarks")]
        public string Status { get; set; } = "";

        public string Username { get; set; } = "";

        [Required]
        public string Area { get; set; } = null!;

        [Required]
        [Display(Name = "Inform to bring out of the Premises the following items:", Prompt = "Enter your items")]
        public string Items { get; set; } = null!;

        [Required]
        public DateTime Schedule { get; set; }

        public bool IsRead { get; set; }

        [Display(Name = "Date Requested")]
        public DateTime DateRequested { get; set; }
    }
}