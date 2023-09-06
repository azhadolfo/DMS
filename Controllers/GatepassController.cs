using DocumentManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace Document_Management.Controllers
{
    public class GatepassController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        //RequestGatepass
        public IActionResult RequestGatepass()
        {
            return View();
        }
    }
}
