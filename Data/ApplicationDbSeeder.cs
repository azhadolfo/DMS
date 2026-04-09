using Document_Management.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Data
{
    public static class ApplicationDbSeeder
    {
        private const string AdminUsername = "azh";
        private const string AdminPassword = "azh";

        public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminExists = await dbContext.Accounts
                .AnyAsync(x => x.Username == AdminUsername, cancellationToken);

            if (adminExists)
            {
                return;
            }

            var adminAccount = new Account
            {
                EmployeeNumber = 9999,
                FirstName = "AZH",
                LastName = "ADMIN",
                Username = AdminUsername,
                Role = "admin",
                Department = "MIS",
                AccessDepartments = string.Empty,
                AccessCompanies = string.Empty,
                ModuleAccess = "DMS",
                IsActive = true
            };

            var passwordHasher = new PasswordHasher<Account>();
            adminAccount.Password = passwordHasher.HashPassword(adminAccount, AdminPassword);

            await dbContext.Accounts.AddAsync(adminAccount, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
