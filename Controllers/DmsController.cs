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
            var username = HttpContext.Session.GetString("username");
            if (!string.IsNullOrEmpty(username))
            {
                return View(new FileDocument());
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

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

                    var filename = Path.GetFileName(file.FileName);
                    var uniquePart = $"{fileDocument.Department}_{fileDocument.DateUploaded:yyyyMMddHHmmssfff}";
                    filename = $"{uniquePart}_{filename}"; // Combine uniquePart with the original filename

                    // Determine the subdirectory based on the selected department
                    var departmentSubdirectory = Path.Combine("Files", fileDocument.Department);

                    // Combine the subdirectory with the web root path
                    var uploadFolderPath = Path.Combine(_hostingEnvironment.WebRootPath, departmentSubdirectory);

                    // Ensure the department-specific subdirectory exists
                    if (!Directory.Exists(uploadFolderPath))
                    {
                        Directory.CreateDirectory(uploadFolderPath);
                    }

                    var filePath = Path.Combine(uploadFolderPath, filename);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream); // Copy the file to the server
                    }

                    
                    fileDocument.Name = filename;
                    fileDocument.Location = filePath;
                    _dbcontext.FileDocuments.Add(fileDocument);
                    _dbcontext.SaveChanges();

                    ViewBag.Success = "File uploaded successfully";

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

        [HttpGet]
        public IActionResult DownloadFile()
        {
            // Retrieve the files from the database and project them into FileViewModel
            var fileViewModels = _dbcontext.FileDocuments
                .Select(file => new FileDocument
                {
                    Name = file.Name,
                    Location = file.Location,
                    DateUploaded = file.DateUploaded,
                    Description = file.Description,
                    Department = file.Department
                })
                .ToList();

            return View(fileViewModels);
        }

    }
}
