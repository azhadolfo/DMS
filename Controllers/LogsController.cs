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
        public async Task<IActionResult> Index()
        {
            var logs = await _dbcontext.Logs.OrderByDescending(u => u.Date).ToListAsync();
            return View(logs);
        }
    }
}
