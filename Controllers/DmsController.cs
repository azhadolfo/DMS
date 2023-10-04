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

        private readonly string? username;

        private readonly string? userRole;

        private readonly bool HasAccess;

        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Inject the services in to another variable
        public DmsController(IWebHostEnvironment hostingEnvironment, ApplicationDbContext context, UserRepo userRepo, IHttpContextAccessor httpContextAccessor)
        {
            _hostingEnvironment = hostingEnvironment;
            _dbcontext = context;
            _userRepo = userRepo;

            // Ensure that HttpContext and the session value are not null
            if (httpContextAccessor.HttpContext != null)
            {
                userRole = httpContextAccessor.HttpContext.Session.GetString("userrole")?.ToLower();
                username = httpContextAccessor.HttpContext.Session.GetString("username");
                var userModuleAccess = httpContextAccessor.HttpContext.Session.GetString("usermoduleaccess");
                var userAccess = !string.IsNullOrEmpty(userModuleAccess) ? userModuleAccess.Split(',') : new string[0];

                if (userRole == "admin" || userAccess.Any(module => module.Trim() == "DMS"))
                {
                    HasAccess = true;
                }
            }
            else
            {
                userRole = null; // or set a default value as needed
                username = null;
            }
        }

        //Get for the Action Dms/Upload
        [HttpGet]
        public IActionResult UploadFile()
        {
            if (!string.IsNullOrEmpty(username))
            {
                if (!HasAccess)
                {
                    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
                }

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
            if (!string.IsNullOrEmpty(username))
            {
                if (!HasAccess)
                {
                    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
                }

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
                        var departmentSubdirectory = Path.Combine("Files", fileDocument.Department, fileDocument.Category);

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
                        LogsModel logs = new(username, $"Upload new file in {fileDocument.Department} Department in Sub Category of {fileDocument.Category}");
                        _dbcontext.Logs.Add(logs);

                        _dbcontext.SaveChanges();

                        TempData["success"] = "File uploaded successfully";

                        return View(fileDocument);
                    }
                    else
                    {
                        TempData["error"] = "Please fill out all the required data.";
                        return View(fileDocument);
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = "Contact MIS: " + ex.Message;
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }

            return View(fileDocument);
        }

        public IActionResult DownloadFile()
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HasAccess)
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            var wwwrootPath = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
            var folders = Directory.GetDirectories(wwwrootPath).Select(Path.GetFileName);
            return View(folders);
        }

        public IActionResult SubCategory(string folderName)
        {
            ViewBag.FolderName = folderName;

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HasAccess)
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

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
            var subcategoriesPath = Path.Combine(wwwrootPath, folderName);
            var categories = Directory.GetDirectories(subcategoriesPath).Select(Path.GetFileName);
            return View(categories);
        }

        public async Task<IActionResult> DisplayFiles(string folderName, string subCategory)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HasAccess)
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            ViewBag.FolderName = folderName;

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
            var folderPath = Path.Combine(wwwrootPath, folderName, subCategory); // wwwroot/Files/Department/SubCategory
            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf").Select(Path.GetFileName);

            // Assuming you have a list of FileDocument objects in your database
            // You can filter them based on folderName and select the relevant properties
            var fileDocuments = await _dbcontext.FileDocuments
                .Where(file => file.Category == subCategory && pdfFiles.Contains(file.Name))
                .Select(file => new FileDocument
                {
                    Name = file.Name,
                    Location = file.Location,
                    DateUploaded = file.DateUploaded,
                    Description = file.Description,
                    Department = file.Department,
                    Username = file.Username,
                    Category = file.Category
                })
                .OrderByDescending(u => u.DateUploaded).ToListAsync();

            return View(fileDocuments);
        }

        //GET the uploaded files
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HasAccess)
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            if (userRole == "admin")
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
            if (!HasAccess)
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }
            var files = await _userRepo.GetUploadedFiles(id);
            return View(files);
        }

        //POST for Editing
        [HttpPost]
        public async Task<IActionResult> Edit(FileDocument model)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HasAccess)
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
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