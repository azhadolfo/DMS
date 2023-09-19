using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Document_Management.Models
{
    public class LogsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Username { get; set; }

        public string Activity { get; set; }

        public DateTime Date { get; set; }

        public LogsModel(string username, string activity)
        {
            Username = username;
            Activity = activity;
            Date = DateTime.Now;
        }
    }
}