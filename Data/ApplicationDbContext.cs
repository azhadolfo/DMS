using DocumentManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Register> Account { get; set; }
    }
}
