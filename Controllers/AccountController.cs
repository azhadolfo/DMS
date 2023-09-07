using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;

namespace Document_Management.Controllers
{
    
    public class AccountController : Controller
    {
        
        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Action for Account/Index
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("userid");
            if (userId.HasValue)
            {
                var users = await _dbcontext.Account.ToListAsync();
                return View(users);
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
              
        }

        //Passing the dbcontext in to another variable
        public AccountController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        //Get for the Action Account/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Register());
        }

        //Post for the Action Account/Create
        [HttpPost]
        public IActionResult Create(Register user)
        {
            if (ModelState.IsValid)
            {
                user.Password = HashPassword(user.Password);
                _dbcontext.Add(user);
                _dbcontext.SaveChanges();
                return RedirectToAction("Index", "Account");
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
                    HttpContext.Session.SetInt32("userid", user.Id); // Store user ID in session
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
            var user = _dbcontext.Account.FirstOrDefault(x => x.Id == id);

            return View(user);
        }

        //Post for the Action Account/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(Register model)
        {
            var user = await _dbcontext.Account.FindAsync(model.Id);
            
            if (user != null)
            {
                user.EmployeeNumber = model.EmployeeNumber; 
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Username = model.Username;
                user.Password = model.Password;
                user.Role = model.Role;

                await _dbcontext.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        // GET: Account/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _dbcontext.Account == null)
            {
                return NotFound();
            }

            var employee = await _dbcontext.Account
                .FirstOrDefaultAsync(m => m.Id == id);
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
            var employee = await _dbcontext.Account.FindAsync(id);
            if (employee != null)
            {
                _dbcontext.Account.Remove(employee);
            }

            await _dbcontext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
