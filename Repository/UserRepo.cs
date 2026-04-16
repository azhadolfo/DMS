using Document_Management.Data;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Repository
{
    public class UserRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public UserRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CheckIfFileExists(string originalFile, CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .FileDocuments
                .AnyAsync(f => f.OriginalFilename == originalFile, cancellationToken);
        }

        public async Task<FileDocument?> GetUploadedFiles(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.FileDocuments
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
    }
}
