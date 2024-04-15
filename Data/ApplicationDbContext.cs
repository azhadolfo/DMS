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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<FileDocument>(f =>
            {
                f.HasIndex(f => f.Name).IsUnique();
                f.HasIndex(f => f.OriginalFilename).IsUnique();
                f.HasIndex(f => f.Company);
                f.HasIndex(f => f.Year);
                f.HasIndex(f => f.Category);
                f.HasIndex(f => f.DateUploaded);
            });

            builder.Entity<LogsModel>(l => l.HasIndex(l => l.Date));
        }
    }
}