using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Document_Management.Models
{
    public class EmployeeID
    {
        [Key]
        public int Id { get; set; }

      
        public int Employee_id { get; set; }
    }
}
