using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace Document_Management.Controllers
{
    public class GatepassController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Passing the dbcontext in to another variable
        public GatepassController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        //RequestGatepass
        [HttpGet]
        public IActionResult Insert()
        {

            return View();
        }

        [HttpPost]
        public IActionResult Insert(RequestGP gpInfo)
        {
            if (ModelState.IsValid)
            {
                // Assuming you have a string representation of the date in "yyyy-MM-dd" format.
                string dateInput = "2023-09-08";
                DateTime scheduleDate = DateTime.ParseExact(dateInput, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();

                // Assign the scheduleDate to the ScheduleDate property of the gpInfo object.
                gpInfo.ScheduleDate = scheduleDate;

                _dbcontext.Gatepass.Add(gpInfo);
                _dbcontext.SaveChanges();
                return RedirectToAction("Insert");
            }

            return View(gpInfo);
        }

        public IActionResult Validator()
        {   
            ViewBag.users = _dbcontext.Gatepass.ToList();
            return View();
        }

    }
}
