namespace DocumentManagement.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public int EmployeeNumber { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
    }
}