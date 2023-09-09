using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document_Management.Models
{
    public class RequestGP
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        [Display(Name = "Contact:")]
        public int Contact { get; set; }

        [Required]
        [Display(Name = "Gatepass Id No.:")]
        public int GatepassId { get; set; }

        [Required]
        [Display(Name = "Schedule Date:")]
        public DateTime ScheduleDate { get; set; }

        [Required]
        [Display(Name = "Purpose:")]
        public string Purpose { get; set; } = null!;
    }
}
