using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class DmsController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        private readonly UserRepo _userRepo;

        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Inject the services in to another variable
        public DmsController(IWebHostEnvironment hostingEnvironment, ApplicationDbContext context, UserRepo userRepo)
        {
            _hostingEnvironment = hostingEnvironment;
            _dbcontext = context;
            _userRepo = userRepo;
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
        public async Task<IActionResult> UploadFile(FileDocument fileDocument, IFormFile file)
        {
            try
            {
                if (ModelState.IsValid && file != null && file.Length > 0)
                {
                    var isFileExist = await _userRepo.CheckIfFileExists(file.FileName);

                    if (isFileExist != null)
                    {
                        TempData["error"] = "This file already exists in our database!";
                        return View(fileDocument);
                    }

                    var username = HttpContext.Session.GetString("username");

                    if (string.IsNullOrEmpty(username))
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    fileDocument.DateUploaded = DateTime.Now;
                    fileDocument.Username = username;
                    fileDocument.OriginalFilename = file.FileName;

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
                    LogsModel logs = new(username, $"Upload new file in {fileDocument.Department} Department");
                    _dbcontext.Logs.Add(logs);

                    _dbcontext.SaveChanges();

                    TempData["success"] = "File uploaded successfully";

                    return View(fileDocument);
                }
                else
                {
                    TempData["error"] = "Please fill out all the required data.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = "Contact MIS: " + ex.Message;
            }

            return View(fileDocument);
        }

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

        public async Task<IActionResult> DisplayFiles(string folderName)
        {
            ViewData["folderName"] = folderName; // Using ViewData

            // Retrieve the user's department from the session or any other method you're using
            var userAccessFolders = HttpContext.Session.GetString("useraccessfolders");

            // Split the userDepartment string into individual department names
            var userDepartments = userAccessFolders.Split(',');

            // Check if any of the user's departments allow access to the specified folderName
            if (!userDepartments.Any(dep => dep.Trim() == folderName))
            {
                TempData["Denied"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("DownloadFile"); // Redirect to the login page or another appropriate action
            }

            var wwwrootPath = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
            var folderPath = Path.Combine(wwwrootPath, folderName); // wwwroot/Files/null
            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf").Select(Path.GetFileName);

            // Assuming you have a list of FileDocument objects in your database
            // You can filter them based on folderName and select the relevant properties
            var fileDocuments = await _dbcontext.FileDocuments
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
                .OrderByDescending(u => u.DateUploaded).ToListAsync();

            return View(fileDocuments);
        }

        //GET the uploaded files
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("username");
            var userrole = HttpContext.Session.GetString("userrole")?.ToLower();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (userrole == "admin")
            {
                var files = await _userRepo.DisplayAllUploadedFiles();
                return View(files);
            }
            else
            {
                var files = await _userRepo.DisplayUploadedFiles(username);
                return View(files);
            }
        }

        //GET for Editing
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var files = await _userRepo.GetUploadedFiles(id);
            return View(files);
        }

        //POST for Editing
        [HttpPost]
        public async Task<IActionResult> Edit(FileDocument model)
        {
            var username = HttpContext.Session.GetString("username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var file = await _dbcontext.FileDocuments
                .FindAsync(model.Id);

            if (file == null)
            {
                return NotFound();
            }

            if (file.Description != model.Description)
            {
                file.Description = model.Description;

                // Implementing the logs
                LogsModel logs = new(username, $"Update the details of file# {file.Id}.");
                _dbcontext.Logs.Add(logs);

                await _dbcontext.SaveChangesAsync();
                TempData["success"] = "File details updated successfully";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Edit");
        }
    }
}