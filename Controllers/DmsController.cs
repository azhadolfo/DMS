using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class DmsController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Passing the dbcontext in to another variable
        public DmsController(IWebHostEnvironment hostingEnvironment,ApplicationDbContext context)
        {
            _hostingEnvironment = hostingEnvironment;
            _dbcontext = context;
        }

        //Get for the Action Dms/Upload
        [HttpGet]
        public IActionResult UploadFile()
        {
            return View(new FileDocument());
        }

        //Post for the Action Dms/Upload
        //[HttpPost]
        //public IActionResult UploadFile(FileDocument fileDocument, IFormFile file)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid && file != null && file.Length > 0)
        //        {
        //            //var username = HttpContext.Session.GetString("username");
        //            fileDocument.DateUploaded = DateTime.Now;
        //            //fileDocument.Username = username;
        //            fileDocument.Username = "test";

        //            var filename = file.FileName;
        //            filename = Path.GetFileName(filename);
        //            var uploadFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "UploadedFiles");
        //            var stream = new FileStream(uploadFilePath, FileMode.Create);
        //            file.CopyToAsync(stream);
        //            fileDocument.Name = filename;
        //            _dbcontext.FileDocuments.Add(fileDocument);
        //            _dbcontext.SaveChanges();
        //            ViewBag.message = "File uploaded successfully";
        //            return RedirectToAction("UploadFile");
        //        }

        //    }   
        //    catch (Exception ex)
        //    {
        //        ViewBag.message = "Error: " + ex.Message.ToString();
        //    }

        //    return View(fileDocument);
        //}

        [HttpPost]
        public IActionResult UploadFile(FileDocument fileDocument, IFormFile file)
        {
            try
            {
                if (ModelState.IsValid && file != null && file.Length > 0)
                {
                    var username = HttpContext.Session.GetString("username");
                    fileDocument.DateUploaded = DateTime.Now;
                    fileDocument.Username = username;

                    //Uncomment this for test data
                    //fileDocument.Username = "test";

                    var filename = Path.GetFileName(file.FileName);
                    var uniquePart = $"{fileDocument.Department}_{fileDocument.DateUploaded:yyyyMMddHHmmssfff}";
                    filename = $"{uniquePart}_{filename}"; // Combine uniquePart with the original filename

                    var uploadFolderPath = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
                    var filePath = Path.Combine(uploadFolderPath, filename);

                    // Ensure the "Files" directory exists
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream); // Copy the file to the server
                    }
                    ViewBag.message = "File uploaded successfully";
                    fileDocument.Name = filename;
                    fileDocument.Location = filePath;
                    _dbcontext.FileDocuments.Add(fileDocument);
                    _dbcontext.SaveChanges();
                    
                    return RedirectToAction("UploadFile");
                }
                else
                {
                    ViewBag.message = "Please select a valid file to upload.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.message = "Error: " + ex.Message.ToString();
            }


            return View(fileDocument);
        }


    }
}
