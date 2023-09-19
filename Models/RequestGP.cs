using System.ComponentModel.DataAnnotations;
using System.Configuration;

namespace Document_Management.Models
{
    public class RequestGP
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Last Name:")]
        public string LastName { get; set; } = null!;

        [Required]
        [Display(Name = "First Name:")]
        public string FirstName { get; set; } = null!;

        [Required]
        [Display(Name = "Middle Name:")]
        public string MiddleName { get; set; } = null!;

        [Required]
        [IntegerValidator]
        [Display(Name = "Contact:")]
        public Int64 Contact { get; set; }

        [Required]
        [Display(Name = "Gatepass Id No.:")]
        public int GatepassId { get; set; }

        //[Required]
        //[Display(Name = "Purpose:")]
        //public string Purpose { get; set; } = null!;

        public string Status { get; set; } = "";

        public string Username { get; set; } = "";

        [Required]
        [Display(Name = "Area:")]
        public string Area { get; set; } = null!;

        [Required]
        public string Items { get; set; } = null!;

        [Required]
        [Display(Name = "Schedule Date:")]
        public DateTime Schedule { get; set; }
    }
}