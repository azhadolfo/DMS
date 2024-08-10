using Document_Management.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class LogsController : Controller
    {
        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Passing the dbcontext in to another variable
        public LogsController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        public async Task<IActionResult> IndexAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return RedirectToAction("Login", "Account");
            }

            var userrole = HttpContext.Session.GetString("userrole");

            //if (userrole != "Admin")
            //{
            //    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
            //    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            //}

            var logs = await _dbcontext.Logs
                .OrderByDescending(u => u.Date)
                .ToListAsync(cancellationToken);

            return View(logs);
        }
    }
}