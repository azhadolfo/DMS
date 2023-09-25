using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;

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

        public IActionResult Index()
        {
            var userrole = HttpContext.Session.GetString("userrole");

            if (userrole != "Admin")
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            //int pageSize = 10; // Number of items per page
            //int pageIndex = page ?? 1; // Default to page 1 if no page number is specified

            var logs = _dbcontext.Logs
                .OrderByDescending(u => u.Date)
                .ToList();

            //var model = await PaginatedList<LogsModel>.CreateAsync(logs, pageIndex, pageSize);

            return View(logs);
        }
    }
}