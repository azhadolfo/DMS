using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class DmsController : Controller
    {
        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Passing the dbcontext in to another variable
        public DmsController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        //Get for the Action Dms/Upload
        [HttpGet]
        public IActionResult UploadFile()
        {
            return View(new FileDocument());
        }

        //Post for the Action Dms/Upload
        [HttpPost]
        public IActionResult UploadFile(FileDocument fileDocument)
        {     
            if (ModelState.IsValid)
            {
                var username = HttpContext.Session.GetString("username");
                fileDocument.DateUploaded = DateTime.Now;
                fileDocument.Username = username;
                _dbcontext.FileDocuments.Add(fileDocument);
                _dbcontext.SaveChanges();
                return RedirectToAction("Index", "Account");
            }

                return View(fileDocument);
                    
        }

    }
}
