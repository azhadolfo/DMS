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

        public async Task<Register> GetUserDetails(string username, string password)
        {
            return await dbContext.Account.FirstOrDefaultAsync(user => user.Username == username && user.Password == password);
        }
    }
}