using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Document_Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AccountController> _logger;
        private readonly string? _userRole;
        private readonly string? _userName;
        private static readonly PasswordHasher<Account> _accountPasswordHasher = new();

        public AccountController(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AccountController> logger)
        {
            _dbContext = context;
            _logger = logger;

            if (httpContextAccessor.HttpContext != null)
            {
                _userRole = httpContextAccessor.HttpContext.Session.GetString("userRole")?.ToLower();
                _userName = httpContextAccessor.HttpContext.Session.GetString("username");
            }
            else
            {
                _userRole = null;
            }
        }

        public async Task<IActionResult> Index()
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            try
            {
                var users = await _dbContext.Accounts
                    .OrderBy(u => u.EmployeeNumber)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load users.");
                TempData["ErrorMessage"] = "Failed to load users.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            try
            {
                return View(new Account
                {
                    Departments = await _dbContext.Departments
                           .OrderBy(d => d.DepartmentName)
                           .Select(s => new SelectListItem
                           {
                               Text = s.DepartmentName,
                               Value = s.DepartmentName
                           })
                           .ToListAsync(),
                    Companies = await _dbContext.Companies
                           .OrderBy(c => c.CompanyName)
                           .Select(s => new SelectListItem
                           {
                               Text = s.CompanyName,
                               Value = s.CompanyName
                           })
                           .ToListAsync()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load user create form.");
                TempData["ErrorMessage"] = "Failed to load user form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Account user, string[] accessDepartments, string[] accessCompanies, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(user);
                }

                var usernameExists = await _dbContext.Accounts
                    .AnyAsync(u => u.Username == user.Username, cancellationToken);

                var employeeNumberExists = await _dbContext.Accounts
                    .AnyAsync(u => u.EmployeeNumber == user.EmployeeNumber, cancellationToken);

                switch (usernameExists)
                {
                    case true when employeeNumberExists:
                        ModelState.AddModelError("", "Both Username and Employee Number are already in use by other users.");
                        break;

                    case true:
                        ModelState.AddModelError("", "Username is already in use by another user.");
                        break;

                    default:
                        if (employeeNumberExists)
                        {
                            ModelState.AddModelError("", "Employee Number is already in use by another user.");
                        }
                        break;
                }

                if (usernameExists || employeeNumberExists)
                {
                    return View(user);
                }

                user.Departments = await _dbContext.Departments
                    .OrderBy(d => d.DepartmentName)
                    .Select(s => new SelectListItem
                    {
                        Text = s.DepartmentName,
                        Value = s.DepartmentName
                    })
                    .ToListAsync(cancellationToken);

                user.Companies = await _dbContext.Companies
                    .OrderBy(c => c.CompanyName)
                    .Select(s => new SelectListItem
                    {
                        Text = s.CompanyName,
                        Value = s.CompanyName
                    })
                    .ToListAsync(cancellationToken);

                user.FirstName = user.FirstName.ToUpper();
                user.LastName = user.LastName.ToUpper();
                user.AccessDepartments = string.Join(",", accessDepartments);
                user.AccessCompanies = string.Join(",", accessCompanies);

                user.Password = HashPassword(user, user.Password);
                await _dbContext.Accounts.AddAsync(user, cancellationToken);

                LogsModel logs = new(_userName!, $"Add new user: {user.Username}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "User created successfully";
                return RedirectToAction("Index", "Account");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create user {Username}.", user.Username);
                TempData["ErrorMessage"] = "Failed to create user.";
                return View(user);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (string.IsNullOrEmpty(_userName))
            {
                return View();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var user = await _dbContext.Accounts
                    .FirstOrDefaultAsync(u => u.Username == userName, cancellationToken);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid username or password");
                    return View();
                }

                var passwordVerification = VerifyPassword(user, password);
                if (!passwordVerification.IsValid)
                {
                    ModelState.AddModelError("", "Invalid username or password");
                    return View();
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError("", "Your account is inactive. Please contact the MIS Department for assistance.");
                    return View();
                }

                HttpContext.Session.SetString("username", user.Username);
                HttpContext.Session.SetString("userRole", user.Role);
                HttpContext.Session.SetString("userAccessDepartments", user.AccessDepartments);
                HttpContext.Session.SetString("userAccessCompanies", user.AccessCompanies);
                HttpContext.Session.SetString("userFirstName", user.FirstName);

                LogsModel logs = new(user.Username, $"Login Successfully");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                if (passwordVerification.NeedsUpgrade)
                {
                    user.Password = HashPassword(user, password);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Login failed for user {Username}.", userName);
                ModelState.AddModelError("", "An unexpected error occurred during login.");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            var user = await _dbContext.Accounts
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (user == null)
            {
                return NotFound();
            }

            user.Departments = await _dbContext.Departments
                .OrderBy(d => d.DepartmentName)
                .Select(s => new SelectListItem
                {
                    Text = s.DepartmentName,
                    Value = s.DepartmentName
                })
                .ToListAsync(cancellationToken);

            user.Companies = await _dbContext.Companies
                .OrderBy(c => c.CompanyName)
                .Select(s => new SelectListItem
                {
                    Text = s.CompanyName,
                    Value = s.CompanyName
                })
                .ToListAsync(cancellationToken);

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Account model,
            string[] accessDepartments,
            string[] accessCompanies,
            string newPassword,
            string newConfirmPassword,
            CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var user = await _dbContext.Accounts
                    .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

                if (user == null)
                {
                    return NotFound();
                }

                user.Departments = await _dbContext.Departments
                    .OrderBy(d => d.DepartmentName)
                    .Select(s => new SelectListItem
                    {
                        Text = s.DepartmentName,
                        Value = s.DepartmentName
                    })
                    .ToListAsync(cancellationToken);

                user.Companies = await _dbContext.Companies
                    .OrderBy(c => c.CompanyName)
                    .Select(s => new SelectListItem
                    {
                        Text = s.CompanyName,
                        Value = s.CompanyName
                    })
                    .ToListAsync(cancellationToken);

                var dataChanged = user.EmployeeNumber != model.EmployeeNumber ||
                                  user.FirstName != model.FirstName ||
                                  user.LastName != model.LastName ||
                                  user.Department != model.Department ||
                                  user.Username != model.Username ||
                                  user.Role != model.Role ||
                                  user.IsActive != model.IsActive ||
                                  (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(newConfirmPassword)) ||
                                  !user.AccessDepartments.Split(',').SequenceEqual(accessDepartments) ||
                                  !user.AccessCompanies.Split(',').SequenceEqual(accessCompanies);

                if (!dataChanged)
                {
                    return RedirectToAction("Edit");
                }

                user.EmployeeNumber = model.EmployeeNumber;
                user.FirstName = model.FirstName.ToUpper();
                user.LastName = model.LastName.ToUpper();
                user.Department = model.Department;
                user.Username = model.Username;
                user.Role = model.Role;
                user.IsActive = model.IsActive;

                if (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(newConfirmPassword))
                {
                    if (newPassword == newConfirmPassword)
                    {
                        user.Password = HashPassword(user, newPassword);
                    }
                    else
                    {
                        TempData["error"] = "Password is not the same";
                        return View(model);
                    }
                }

                user.AccessDepartments = accessDepartments.Length > 0 ? string.Join(",", accessDepartments) : string.Empty;
                user.AccessCompanies = accessCompanies.Length > 0 ? string.Join(",", accessCompanies) : string.Empty;

                LogsModel logs = new(_userName!, $"Update user: {user.Username}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "User updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update user {UserId}.", model.Id);
                TempData["error"] = "Failed to update user.";
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            if (id == null)
            {
                return NotFound();
            }

            var employee = await _dbContext.Accounts
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var employee = await _dbContext.Accounts
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

                if (employee == null)
                {
                    return RedirectToAction(nameof(Index));
                }

                _dbContext.Accounts.Remove(employee);

                LogsModel logs = new(_userName!, $"Delete user: {employee.Username}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "User deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to delete user {UserId}.", id);
                TempData["error"] = "Failed to delete user.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (string.IsNullOrEmpty(_userName))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(Account model, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_userName))
            {
                return RedirectToAction("Login", "Account");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var user = await _dbContext.Accounts
                    .FirstOrDefaultAsync(x => x.Username == _userName, cancellationToken);

                if (user == null)
                {
                    return NotFound();
                }

                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    TempData["error"] = "Password is required.";
                    return View();
                }

                if (VerifyPassword(user, model.Password).IsValid)
                {
                    TempData["error"] = "New password must not the same with the previous.";
                    return View();
                }

                user.Password = HashPassword(user, model.Password);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "Change password successfully";
                return View();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to change password for user {Username}.", _userName);
                TempData["error"] = "Failed to change password.";
                return View();
            }
        }

        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_userName))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index", "Home");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                LogsModel logs = new(_userName!, $"Logout Successfully");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                HttpContext.Session.Clear();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to logout user {Username}.", _userName);
                TempData["error"] = "Failed to logout.";
                return RedirectToAction("Index", "Home");
            }
        }

        private IActionResult? EnsureAdminAccess()
        {
            if (string.IsNullOrEmpty(_userName))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_userRole != "admin")
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }

            return null;
        }

        private static string HashPassword(Account user, string password)
        {
            return _accountPasswordHasher.HashPassword(user, password);
        }

        private static (bool IsValid, bool NeedsUpgrade) VerifyPassword(Account user, string password)
        {
            var verificationResult = _accountPasswordHasher.VerifyHashedPassword(user, user.Password, password);
            if (verificationResult == PasswordVerificationResult.Success)
            {
                return (true, false);
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return (true, true);
            }

            var legacyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            var legacyMatch = legacyHash == user.Password;
            return (legacyMatch, legacyMatch);
        }
    }
}
