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
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }

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
            builder.Entity<Company>(c => c.HasIndex(c => c.CompanyName));
            builder.Entity<Department>(d => d.HasIndex(d => d.DepartmentName));
            builder.Entity<Category>(c => c.HasIndex(c => c.CategoryName));
            builder.Entity<SubCategory>(sc =>
            {
                sc.HasIndex(sc => sc.SubCategoryName);
                sc.HasOne(c => c.Category)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(sc => sc.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            
        }
    }
}