using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Document_Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly string? _userRole;
        private readonly string? _userName;
        
        public AccountController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = context;
            
            if (httpContextAccessor.HttpContext != null)
            {
                _userRole = httpContextAccessor.HttpContext.Session.GetString("userrole")?.ToLower();
                _userName = httpContextAccessor.HttpContext.Session.GetString("username");
            }
            else
            {
                _userRole = null;
            }
        }
        
        public async Task<IActionResult> Index()
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

            var users = await _dbContext.Account
                .OrderBy(u => u.EmployeeNumber)
                .ToListAsync();

            return View(users);

        }
        
        [HttpGet]
        public IActionResult Create()
        {
            if (!string.IsNullOrEmpty(_userName))
            {
                return View(new Register());
            }

            if (_userRole == "admin")
            {
                return RedirectToAction("Login", "Account");
            }
            
            TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");

        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Register user, string[] accessFolders, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }
            
            var usernameExists = await _dbContext.Account
                .AnyAsync(u => u.Username == user.Username, cancellationToken);

            var employeeNumberExists = await _dbContext.Account
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
                {
                    if (employeeNumberExists)
                    {
                        ModelState.AddModelError("", "Employee Number is already in use by another user.");
                    }

                    break;
                }
            }

            if (usernameExists || employeeNumberExists)
            {
                return View(user);
            }

            if (string.IsNullOrEmpty(_userName))
            {
                return RedirectToAction("Login", "Account");
            }
            
            user.AccessFolders = string.Join(",", accessFolders);

            user.Password = HashPassword(user.Password);
            await _dbContext.Account.AddAsync(user, cancellationToken);
            
            LogsModel logs = new(_userName, $"Add new user: {user.Username}");
            await _dbContext.Logs.AddAsync(logs, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["success"] = "User created successfully";
            return RedirectToAction("Index", "Account");

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
            
            var user = await _dbContext.Account
                .FirstOrDefaultAsync(u => u.Username == userName, cancellationToken);

            if (user != null && user.Password == HashPassword(password))
            {
                HttpContext.Session.SetString("username", user.Username);
                HttpContext.Session.SetString("userrole", user.Role);
                HttpContext.Session.SetString("useraccessfolders", user.AccessFolders);
                HttpContext.Session.SetString("usermoduleaccess", user.ModuleAccess);
                HttpContext.Session.SetString("userfirstname", user.FirstName);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid username or password");

            return View();
        }
        
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_userName))
            {
                if (_userRole == "admin")
                {
                    return RedirectToAction("Login", "Account");
                }
                
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }
            
            var user = await _dbContext.Account
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (user == null)
            {
                return NotFound();
            }
            
            if (string.IsNullOrEmpty(user.AccessFolders))
            {
                return View(user);
            }
            
            var selectedDepartments = user.AccessFolders.Split(',').ToList();
            
            user.AccessFolders = string.Join(",", selectedDepartments);

            return View(user);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Register model,
            string[] accessFolders,
            string newPassword,
            string newConfirmPassword,
            CancellationToken cancellationToken)
        {
            var user = await _dbContext.Account
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (string.IsNullOrEmpty(_userName))
            {
                if (_userRole == "admin")
                {
                    return RedirectToAction("Login", "Account");
                }
                
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }

            if (user == null)
            {
                return RedirectToAction("Index");
            }
            
            var dataChanged = user.EmployeeNumber != model.EmployeeNumber ||
                              user.FirstName != model.FirstName ||
                              user.LastName != model.LastName ||
                              user.Department != model.Department ||
                              user.Username != model.Username ||
                              user.Role != model.Role ||
                              (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(newConfirmPassword)) ||
                              !user.AccessFolders.Split(',').SequenceEqual(accessFolders);

            if (!dataChanged)
            {
                return RedirectToAction("Edit");
            }
            
            user.EmployeeNumber = model.EmployeeNumber;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Department = model.Department;
            user.Username = model.Username;
            user.Role = model.Role;
            
            if (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(newConfirmPassword))
            {
                if (newPassword == newConfirmPassword)
                {
                    user.Password = HashPassword(newPassword);
                }
                else
                {
                    TempData["error"] = "Password is not the same";
                    return View(model);
                }
            }

            user.AccessFolders = accessFolders.Length > 0 ? string.Join(",", accessFolders) : string.Empty;
            
            LogsModel logs = new(_userName, $"Update user: {user.Username}");
            await _dbContext.Logs.AddAsync(logs, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["success"] = "User updated successfully";
            return RedirectToAction("Index");

        }
        
        public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_userName))
            {
                if (_userRole == "admin")
                {
                    return RedirectToAction("Login", "Account");
                }
                
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var employee = await _dbContext.Account
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
            if (string.IsNullOrEmpty(_userName))
            {
                if (_userRole == "admin")
                {
                    return RedirectToAction("Login", "Account");
                }
                
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");

            }

            var employee = await _dbContext.Account
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (employee != null)
            {
                _dbContext.Account.Remove(employee);
                
                LogsModel logs = new(_userName, $"Delete user: {employee.Username}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "User deleted successfully";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(Register model, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Account
                .FirstOrDefaultAsync(x => x.Username == _userName, cancellationToken);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Password == HashPassword(model.Password))
            {
                TempData["error"] = "New password must not the same with the previous.";
            }

            user.Password = HashPassword(model.Password);
            await _dbContext.SaveChangesAsync(cancellationToken);

            TempData["success"] = "Change password successfully";
            return View();
        }
        
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
        
        private static string HashPassword(string password)
        {
            var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}