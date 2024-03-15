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

        public async Task<Register?> GetUserDetails(string username, string password, CancellationToken cancellationToken = default)
        {
            return await dbContext.Account.FirstOrDefaultAsync(user => user.Username == username && user.Password == password, cancellationToken);
        }

        public async Task<bool> CheckIfFileExists(string originalfile, CancellationToken cancellationToken = default)
        {
            return await dbContext
                .FileDocuments
                .AnyAsync(f => f.OriginalFilename == originalfile, cancellationToken);
        }

        public async Task<List<FileDocument>> DisplayAllUploadedFiles(CancellationToken cancellationToken = default)
        {
            return await dbContext.FileDocuments
                .ToListAsync(cancellationToken);
        }

        public async Task<List<FileDocument>> DisplayUploadedFiles(string username, CancellationToken cancellationToken = default)
        {
            return await dbContext.FileDocuments
                .Where(file => file.Username == username)
                .ToListAsync(cancellationToken);
        }

        public async Task<FileDocument?> GetUploadedFiles(int id, CancellationToken cancellationToken = default)
        {
            return await dbContext.FileDocuments.FindAsync(id, cancellationToken);
        }

        public List<FileDocument> SearchFile(string[] keywords)
        {
            return dbContext.FileDocuments
                .AsEnumerable() // Switch to client-side evaluation
                .Where(f => keywords.All(k => f.Description.Contains(k, StringComparison.CurrentCultureIgnoreCase)))
                .ToList();
        }
    }
}