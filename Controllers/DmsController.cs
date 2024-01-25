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
        [ValidateAntiForgeryToken]
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
                        if (file.ContentType != "application/pdf")
                        {
                            TempData["error"] = "Please upload pdf file only!";
                            return View(fileDocument);
                        }

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

                        string departmentSubdirectory;

                        if (fileDocument.SubCategory == null)
                        {
                            // Determine the subdirectory based on the selected department
                            departmentSubdirectory = Path.Combine("Files", fileDocument.Company, fileDocument.Year, fileDocument.Department, fileDocument.Category);
                            fileDocument.SubCategory = "N/A";
                        }
                        else
                        {
                            departmentSubdirectory = Path.Combine("Files", fileDocument.Company, fileDocument.Year, fileDocument.Department, fileDocument.Category, fileDocument.SubCategory);
                        }

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
                        LogsModel logs = new(username, $"Uploaded in {fileDocument.Department}/{fileDocument.Category} {fileDocument.NumberOfPages} page(s).");
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

        public IActionResult CompanyFolder(string folderName)
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

            var wwwrootPath = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
            var companyFolderPath = Path.Combine(wwwrootPath, folderName);
            var company = Directory.GetDirectories(companyFolderPath).Select(Path.GetFileName);
            return View(company);
        }

        public IActionResult YearFolder(string companyFolderName, string yearFolderName)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;

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
            var yearFolderPath = Path.Combine(wwwrootPath, companyFolderName, yearFolderName);
            var year = Directory.GetDirectories(yearFolderPath).Select(Path.GetFileName);
            return View(year);
        }

        public IActionResult DepartmentFolder(string departmentFolderName, string companyFolderName, string yearFolderName)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;

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

            // Check if any of the user's departments allow access to the specified companyFolderName
            if (!userDepartments.Any(dep => dep.Trim() == departmentFolderName))
            {
                TempData["Denied"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("YearFolder", new { companyFolderName = companyFolderName, yearFolderName = yearFolderName }); // Redirect to the login page or another appropriate action
            }

            var wwwrootPath = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
            var departmentFolderPath = Path.Combine(wwwrootPath, companyFolderName, yearFolderName, departmentFolderName);
            var department = Directory.GetDirectories(departmentFolderPath).Select(Path.GetFileName);
            return View(department);
        }

        public IActionResult SubCategoryFolder(string documentTypeFolderName, string departmentFolderName, string companyFolderName, string yearFolderName)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;
            ViewBag.DocumentTypeFolder = documentTypeFolderName;

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
            var documentTypeFolderPath = Path.Combine(wwwrootPath, companyFolderName, yearFolderName, departmentFolderName, documentTypeFolderName);
            var documentType = Directory.GetDirectories(documentTypeFolderPath).Select(Path.GetFileName);
            return View(documentType);
        }

        public async Task<IActionResult> DisplayFiles(string departmentFolderName, string companyFolderName, string yearFolderName, string documentTypeFolderName, string? subCategoryFolder)
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

            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;
            ViewBag.CurrentFolder = documentTypeFolderName;

            var cleanDepartmentName = departmentFolderName.Replace("_", " ");

            // Retrieve the user's department from the session or any other method you're using
            var userAccessFolders = HttpContext.Session.GetString("useraccessfolders");

            // Split the userDepartment string into individual department names
            var userDepartments = userAccessFolders.Split(',');

            // Check if any of the user's departments allow access to the specified companyFolderName
            if (!userDepartments.Any(dep => dep.Trim() == departmentFolderName))
            {
                TempData["Denied"] = $"You have no access to {cleanDepartmentName}. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("YearFolder", new { companyFolderName = companyFolderName, yearFolderName = yearFolderName }); // Redirect to the login page or another appropriate action
            }

            var wwwrootPath = Path.Combine(_hostingEnvironment.WebRootPath, "Files");
            string folderPath;
            if (subCategoryFolder == null)
            {
                folderPath = Path.Combine(wwwrootPath, companyFolderName, yearFolderName, departmentFolderName, documentTypeFolderName); // wwwroot/Files/
            }
            else
            {
                folderPath = Path.Combine(wwwrootPath, companyFolderName, yearFolderName, departmentFolderName, documentTypeFolderName, subCategoryFolder);
            }

            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf").Select(Path.GetFileName);

            // Assuming you have a list of FileDocument objects in your database
            // You can filter them based on companyFolderName and select the relevant properties
            var fileDocuments = await _dbcontext.FileDocuments
                .Where(file => file.Company == companyFolderName && file.Year == yearFolderName && file.Category == documentTypeFolderName && pdfFiles.Contains(file.Name))
                .Select(file => new FileDocument
                {
                    Name = file.Name,
                    Location = file.Location,
                    DateUploaded = file.DateUploaded,
                    Description = file.Description,
                    Department = file.Department,
                    Username = file.Username,
                    Category = file.Category,
                    Company = file.Company,
                    Year = file.Year,
                    SubCategory = file.SubCategory,
                    OriginalFilename = file.OriginalFilename
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
        [ValidateAntiForgeryToken]
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

            if (file.Description != model.Description || file.NumberOfPages != model.NumberOfPages)
            {
                file.Description = model.Description;
                file.NumberOfPages = model.NumberOfPages;

                // Implementing the logs
                LogsModel logs = new(username, $"Update the details of file# {file.Id}.");
                _dbcontext.Logs.Add(logs);

                await _dbcontext.SaveChangesAsync();
                TempData["success"] = "File details updated successfully";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Edit");
        }

        public async Task<IActionResult> GeneralSearch(string search)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var keywords = search.Split(' ');

            var result = await _userRepo
                .SearchFileAsync(keywords);

            return View(result);
        }

        public async Task<IActionResult> Delete(int id)
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

            if (id == 0)
            {
                return NotFound();
            }

            var model = await _dbcontext
                .FileDocuments
                .FindAsync(id);

            if (model != null)
            {
                try
                {
                    if (System.IO.File.Exists(model.Location))
                    {
                        System.IO.File.Delete(model.Location);
                    }

                    _dbcontext.Remove(model);

                    // Implementing the logs
                    LogsModel logs = new(username, $"Delete the file: {model.Name}.");
                    _dbcontext.Logs.Add(logs);

                    await _dbcontext.SaveChangesAsync();
                    TempData["success"] = "File has been deleted.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    return RedirectToAction("Error");
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Transfer(int id)
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

            var files = await _userRepo.GetUploadedFiles(id);

            if (files != null)
            {
                return View(files);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(FileDocument model)
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

            var existingModel = await _dbcontext
                .FileDocuments
                .FindAsync(model.Id);

            if (existingModel != null)
            {
                existingModel.Company = model.Company;
                existingModel.Year = model.Year;
                existingModel.Department = model.Department;
                existingModel.Category = model.Category;
                existingModel.SubCategory = model.SubCategory;

                var filename = existingModel.OriginalFilename;
                var uniquePart = $"{model.Department}_{existingModel.DateUploaded:yyyyMMddHHmmssfff}";
                filename = $"{uniquePart}_{filename}";

                existingModel.Name = filename;

                string newPath;

                if (model.SubCategory == null)
                {
                    // Determine the subdirectory based on the selected department
                    newPath = Path.Combine("Files", model.Company, model.Year, model.Department, model.Category);
                    existingModel.SubCategory = "N/A";
                }
                else
                {
                    newPath = Path.Combine("Files", model.Company, model.Year, model.Department, model.Category, model.SubCategory);
                }

                // Combine the subdirectory with the web root path
                var uploadFolderPath = Path.Combine(_hostingEnvironment.WebRootPath, newPath);

                // Ensure the department-specific subdirectory exists
                if (!Directory.Exists(uploadFolderPath))
                {
                    Directory.CreateDirectory(uploadFolderPath);
                }

                var filePath = Path.Combine(uploadFolderPath, filename);

                if (System.IO.File.Exists(existingModel.Location))
                {
                    System.IO.File.Move(existingModel.Location, filePath);
                }

                // Implementing the logs
                LogsModel logs = new(username, $"Transfer the file: {model.OriginalFilename}.");
                _dbcontext.Logs.Add(logs);

                existingModel.Location = filePath;

                await _dbcontext.SaveChangesAsync();

                TempData["success"] = "File successfully transfered.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return NotFound();
            }
        }
    }
}