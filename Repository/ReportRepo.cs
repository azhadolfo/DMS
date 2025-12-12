using Document_Management.Data;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Repository
{
    public class ReportRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ReportRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<FileUploadReportViewModel>> GenerateUploadedFiles(DateOnly DateFrom, DateOnly DateTo, CancellationToken cancellation = default)
        {
            return await _dbContext.FileDocuments
                .Where(f => DateOnly.FromDateTime(f.DateUploaded) >= DateFrom && DateOnly.FromDateTime(f.DateUploaded) <= DateTo)
                .GroupBy(f => new
                {
                    f.BoxNumber,
                    f.Company,
                    f.Year,
                    f.Department,
                    f.Category,
                    f.SubCategory,
                    f.Username,
                    f.SubmittedBy,
                    SubmittedDate = f.DateSubmitted,
                })
                .Select(g => new FileUploadReportViewModel
                {
                    BoxNumber = g.Key.BoxNumber,
                    Company = g.Key.Company,
                    Year = g.Key.Year,
                    Department = g.Key.Department,
                    Category = g.Key.Category,
                    SubCategory = g.Key.SubCategory,
                    Username = g.Key.Username,
                    FileCount = g.Count(),
                    SubmittedBy = g.Key.SubmittedBy,
                    DateSubmitted = g.Key.SubmittedDate,
                })
                .ToListAsync(cancellation);
        }
    }
}
