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

        [HttpGet]
        public IActionResult Approved(int id) 
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id); 

            if (requestGP == null)
            {
                return NotFound(); 
            }

            return View(requestGP);
        }


        [HttpGet]
        public IActionResult Disapproved(int id)
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);

            if (requestGP == null)
            {
                return NotFound();
            }

            return View(requestGP);
        }


        [HttpPost]
        public IActionResult Disapproved()
        {
           
           
            return View();
        }

    }
}
