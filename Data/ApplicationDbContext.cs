using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Register> Account { get; set; }
        public DbSet<FileDocument> FileDocuments { get; set; }
        public DbSet<LogsModel> Logs { get; set; }

        public DbSet<RequestGP> Gatepass { get; set; }
        public DbSet<HubConnection> HubConnections { get; set; }
    }
}