using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Document_Management.Controllers
{
    
    public class AccountController : Controller
    {

        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Passing the dbcontext in to another variable
        public AccountController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        //Action for Account/Index
        public async Task<IActionResult> Index() 
        {
            var username = HttpContext.Session.GetString("username");
            if (!string.IsNullOrEmpty(username))
            {

                //int pageSize = 10; // Number of items per page
                //int pageIndex = page ?? 1; // Default to page 1 if no page number is specified

                var users = await _dbcontext.Account.OrderBy(u => u.EmployeeNumber).ToListAsync();

                //var model = await PaginatedList<Register>.CreateAsync(users, pageIndex, pageSize);

                return View(users);
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }


        //Get for the Action Account/Create
        [HttpGet]
        public IActionResult Create()
        {
            var userrole = HttpContext.Session.GetString("userrole")?.ToLower();

            if (!(userrole == "admin"))
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            return View(new Register());
        }

        //Post for the Action Account/Create
        [HttpPost]
        public IActionResult Create(Register user, string[] AccessFolders)
        {
            if (ModelState.IsValid)
            {
                var existingUser = _dbcontext.Account.FirstOrDefault(u => u.Username == user.Username);

                if(existingUser != null)
                {
                    ModelState.AddModelError("", "Username is already in use by other user.");
                    return View(existingUser);
                }

                var username = HttpContext.Session.GetString("username");

                if (!string.IsNullOrEmpty(username))
                {
                    // Join selected departments into a comma-separated string
                    user.AccessFolders = string.Join(",", AccessFolders);

                    user.Password = HashPassword(user.Password);
                    user.ConfirmPassword = HashPassword(user.ConfirmPassword);
                    _dbcontext.Account.Add(user);

                    //Implementing the logs 
                    LogsModel logs = new(username, $"Add new user: {user.Username}");
                    _dbcontext.Logs.Add(logs);

                    _dbcontext.SaveChanges();
                    TempData["success"] = "User created successfully";
                    return RedirectToAction("Index", "Account");
                }
                else
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            return View(user);
        }

        //Get for Action Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //Post for Action Account/Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (ModelState.IsValid)
            {
                var user = _dbcontext.Account.FirstOrDefault(u => u.Username == username);
                if(user!=null && user.Password == HashPassword(password))
                {
                    HttpContext.Session.SetString("username", user.Username); // Store username in session
                    HttpContext.Session.SetString("userrole", user.Role); // Store user role in session
                    HttpContext.Session.SetString("useraccessfolders", user.AccessFolders); // Store user role in session



                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("","Invalid username or password");
                }


            }

            return View();
        }

        //Get for the Action Account/Edit
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userrole = HttpContext.Session.GetString("userrole")?.ToLower();
            if (!(userrole == "admin"))
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            // Retrieve the user from the database
            var user = _dbcontext.Account.FirstOrDefault(x => x.Id == id);

            // Split the comma-separated AccessFolders into a list of selected departments
            if (!string.IsNullOrEmpty(user.AccessFolders))
            {
                var selectedDepartments = user.AccessFolders.Split(',').ToList();

                // Join the selected departments back into a comma-separated string
                user.AccessFolders = string.Join(",", selectedDepartments);
            }

            return View(user);
        }



        //Post for the Action Account/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(Register model, string[] AccessFolders, string newPassword, string newConfirmPassword)
        {
            var user = await _dbcontext.Account.FindAsync(model.Id);
            var username = HttpContext.Session.GetString("username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (user != null)
            {
                // Update the user properties
                user.EmployeeNumber = model.EmployeeNumber;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Department = model.Department;
                user.Username = model.Username;
                user.Role = model.Role;

                // Check if a new password is provided
                if (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(newConfirmPassword))
                {
                    // Hash and update the new password
                    user.Password = HashPassword(newPassword);
                    user.Password = HashPassword(newConfirmPassword);
                }

                // Join the selected departments into a comma-separated string
                if (AccessFolders != null && AccessFolders.Length > 0)
                {
                    user.AccessFolders = string.Join(",", AccessFolders);
                }
                else
                {
                    // Handle the case where no departments are selected
                    user.AccessFolders = string.Empty;
                }

                // Implementing the logs
                LogsModel logs = new(username, $"Update user: {user.Username}");
                _dbcontext.Logs.Add(logs);

                await _dbcontext.SaveChangesAsync();
                TempData["success"] = "User updated successfully";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }


        // GET: Account/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var userrole = HttpContext.Session.GetString("userrole")?.ToLower();
            if (!(userrole == "admin"))
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            if (id == null || _dbcontext.Account == null)
            {
                return NotFound();
            }

            var employee = await _dbcontext.Account.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }


        // POST: Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_dbcontext.Account == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Account'  is null.");
            }

            var username = HttpContext.Session.GetString("username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _dbcontext.Account.FindAsync(id);
            if (employee != null)
            {
                _dbcontext.Account.Remove(employee);

                //Implementing the logs 
                LogsModel logs = new(username, $"Delete user: {employee.Username}");
                _dbcontext.Logs.Add(logs);

                await _dbcontext.SaveChangesAsync();

                TempData["success"] = "User deleted successfully";
            }

            await _dbcontext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(Register model)
        {
             var username = HttpContext.Session.GetString("username")?.ToLower();
             var user = await _dbcontext.Account.FirstOrDefaultAsync(x => x.Username == username);

             if (user != null)
             {
                if(user.Password == HashPassword(model.Password))
                {
                    TempData["error"] = "New password must not the same with the previous.";
                }

                user.Password = HashPassword(model.Password);
                user.ConfirmPassword = HashPassword(model.ConfirmPassword);
                await _dbcontext.SaveChangesAsync();
            }

             TempData["success"] = "Change password successfully";
             return View();
         
        }

        //Action for Account/Logout and remove the session 
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Redirect to the login page or any other appropriate page
            return RedirectToAction("Index","Home");
        }


        // Hash the password using a salt
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

    }
}
