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

        public async Task<Register?> GetUserDetails(string username, string password)
        {
            return await dbContext.Account.FirstOrDefaultAsync(user => user.Username == username && user.Password == password);
        }

        public async Task<FileDocument?> CheckIfFileExists(string originalfile)
        {
            return await dbContext
                .FileDocuments
                .FirstOrDefaultAsync(file => file.OriginalFilename == originalfile);
        }

        public async Task<List<FileDocument>> DisplayAllUploadedFiles()
        {
            return await dbContext.FileDocuments
                .ToListAsync();
        }

        public async Task<List<FileDocument>> DisplayUploadedFiles(string username)
        {
            return await dbContext.FileDocuments
                .Where(file => file.Username == username)
                .ToListAsync();
        }

        public async Task<FileDocument?> GetUploadedFiles(int id)
        {
            return await dbContext.FileDocuments.FindAsync(id);
        }

        public List<FileDocument> SearchFile(string[] keywords)
        {
            var results = dbContext.FileDocuments
                .AsEnumerable() // Switch to client-side evaluation
                .Where(f => keywords.All(k => f.Description.ToUpper().Contains(k.ToUpper())))
                .ToList();

            return results;
        }
    }
}