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

                    if (string.IsNullOrEmpty(username))
                    {
                        return RedirectToAction("Login", "Account");
                    }

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

                    //Implementing the logs 
                    LogsModel logs = new(username, Environment.MachineName, $"Upload new file in {fileDocument.Department} Department");
                    _dbcontext.Logs.Add(logs);

                    _dbcontext.SaveChanges();

                    TempData["success"] = "File uploaded successfully";

                    return RedirectToAction("UploadFile");
                }
                else
                {
                    TempData["error"] = "Please fill out all the required data.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = "Contact MIS: " + ex.Message.ToString();
            }

            return View(fileDocument);
        }

        //[HttpGet]
        //public async Task<IActionResult> DownloadFile(int? page)
        //{
        //    int pageSize = 10; // Number of items per page
        //    int pageIndex = page ?? 1; // Default to page 1 if no page number is specified

        //    // Retrieve the files from the database and project them into FileViewModel
        //    var fileViewModels = _dbcontext.FileDocuments
        //        .Select(file => new FileDocument
        //        {
        //            Name = file.Name,
        //            Location = file.Location,
        //            DateUploaded = file.DateUploaded,
        //            Description = file.Description,
        //            Department = file.Department
        //        })
        //        .OrderBy(u => u.Department);

        //    var model = await PaginatedList<FileDocument>.CreateAsync(fileViewModels, pageIndex, pageSize);

        //    return View(model);
        //}


        public IActionResult DownloadFile()
        {
            var username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var wwwrootPath = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
            var folders = Directory.GetDirectories(wwwrootPath).Select(Path.GetFileName);
            return View(folders);
        }

        public async Task<IActionResult> DisplayFiles(string folderName, int? page)
        {

            // Retrieve the user's department from the session or any other method you're using
            var userDepartment = HttpContext.Session.GetString("userdepartment");
            //var userDepartment = "Marketing";

            // Check if the user's department matches the folder name
            if (userDepartment != folderName)
            {
                TempData["Denied"] = "You have no access to this action. Please contact MIS Department.";
                return RedirectToAction("DownloadFile"); // Redirect to the login page or another appropriate action
            }

            int pageSize = 10; // Number of items per page
            int pageIndex = page ?? 1; // Default to page 1 if no page number is specified

            var wwwrootPath = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
            var folderPath = Path.Combine(wwwrootPath, folderName);
            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf").Select(Path.GetFileName);

            // Assuming you have a list of FileDocument objects in your database
            // You can filter them based on folderName and select the relevant properties
            var fileDocuments = _dbcontext.FileDocuments
                .Where(file => file.Department == folderName && pdfFiles.Contains(file.Name))
                .Select(file => new FileDocument
                {
                    Name = file.Name,
                    Location = file.Location,
                    DateUploaded = file.DateUploaded,
                    Description = file.Description,
                    Department = file.Department,
                    Username = file.Username
                })
                .OrderByDescending(u => u.DateUploaded);

            var model = await PaginatedList<FileDocument>.CreateAsync(fileDocuments, pageIndex, pageSize);

            return View(model);
        }


    }
}
