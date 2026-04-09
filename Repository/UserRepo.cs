using Document_Management.Data;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Repository
{
    public class UserRepo
    {
        private readonly ApplicationDbContext dbContext;

        public UserRepo(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<bool> CheckIfFileExists(string originalfile, CancellationToken cancellationToken = default)
        {
            return await dbContext
                .FileDocuments
                .AnyAsync(f => f.OriginalFilename == originalfile, cancellationToken);
        }

        public async Task<FileDocument?> GetUploadedFiles(int id, CancellationToken cancellationToken = default)
        {
            return await dbContext.FileDocuments.FindAsync(id, cancellationToken);
        }
    }
}
