﻿using Document_Management.Data;
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
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 10; // Number of items per page
            int pageIndex = page ?? 1; // Default to page 1 if no page number is specified

            var logs = _dbcontext.Logs.OrderByDescending(u => u.Date);

            var model = await PaginatedList<LogsModel>.CreateAsync(logs, pageIndex, pageSize);

            return View(model);
        }

    }
}