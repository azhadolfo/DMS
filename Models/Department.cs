using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Document_Management.Utility.Helper;

namespace Document_Management.Models;

public class Department
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column(TypeName = "varchar(100)")]
    [Display(Name = "Department")]
    public string DepartmentName { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string CreatedBy { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();
    
    [Column(TypeName = "varchar(100)")]
    public string? EditedBy { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? EditedDate { get; set; }
}