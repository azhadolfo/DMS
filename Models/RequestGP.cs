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
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [Required]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "First Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in First Name.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "Last Name must be between 1 and 40 characters.")]
        [RegularExpression(@"^[a-zA-Z''-'\s]+$", ErrorMessage = "Invalid characters in Middle Name.")]
        [Display(Name = "Middle Name")]
        public string MiddleName { get; set; } = null!;

        [Required]
        [RegularExpression(@"^((\+63)|)\d{10}$", ErrorMessage = "Contact Number must be a valid Philippine phone number.")]
        [Display(Name = "Contact")]
        public long Contact { get; set; }

        [Required]
        [Display(Name = "Gatepass Id")]
        public int GatepassId { get; set; }

        [Display(Name = "Remarks")]
        public string Status { get; set; } = "";

        [Display(Name = "Username")]
        public string Username { get; set; } = "";

        [Required]
        [Display(Name = "Area")]
        public string Area { get; set; } = null!;

        [Required]
        public string Items { get; set; } = null!;

        [Required]
        [Display(Name = "Date & Time")]
        public DateTime Schedule { get; set; }

        public bool IsRead { get; set; }
    }
}