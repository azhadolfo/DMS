using System.Linq.Dynamic.Core;
using Document_Management.Data;
using Document_Management.Models;
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

            var logs = await _dbcontext.Logs
                .OrderByDescending(u => u.Date)
                .ToListAsync(cancellationToken);

            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> GetActivityLogs([FromForm] DataTablesParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var logs = await _dbcontext.Logs
                    .OrderByDescending(u => u.Date)
                    .ToListAsync(cancellationToken);
                
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    logs = logs
                        .Where(s =>
                            s.Username.ToLower().Contains(searchValue) ||
                            s.Activity.ToLower().Contains(searchValue) ||
                            s.Date.ToString().Contains(searchValue)
                        )
                        .ToList();
                }
                
                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    logs = logs
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }
                
                var totalRecords = logs.Count();

                var pagedData = logs
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}