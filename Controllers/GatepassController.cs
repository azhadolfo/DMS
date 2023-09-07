using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Document_Management.Controllers
{
    public class GatepassController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Passing the dbcontext in to another variable
        public GatepassController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        //RequestGatepass
        [HttpGet]
        public IActionResult RequestGatepass()
        {

            return View(new RequestGatepass());
        }

    }
}
