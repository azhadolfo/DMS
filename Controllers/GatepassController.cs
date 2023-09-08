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
                if (gpInfo.GatepassId != 0)
                {
                    gpInfo.GatepassId = GenerateRandomNumber();
                }

                // representation of the date in "yyyy-MM-dd" format.
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
        private int GenerateRandomNumber()
        {
            Random random = new Random();
            return random.Next(1000, 10000); // Generate a random number between 1000 and 9999 (adjust as needed)
        }

    }
}
