using System.Diagnostics;
using System.Globalization;
using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Document_Management.Services;
using Document_Management.Utility.Helper;
using Google.Cloud.Storage.V1;

namespace Document_Management.Controllers
{
    public class DmsController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly UserRepo _userRepo;
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICloudStorageService _cloudStorage;

        public DmsController(
            IWebHostEnvironment hostingEnvironment, 
            ApplicationDbContext context, 
            UserRepo userRepo, 
            ILogger<HomeController> logger,
            ICloudStorageService cloudStorage)
        {
            _hostingEnvironment = hostingEnvironment;
            _dbContext = context;
            _userRepo = userRepo;
            _logger = logger;
            _cloudStorage = cloudStorage;
        }

        public IActionResult? CheckAccess()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return RedirectToAction("Login", "Account");
            }

            var userRole = HttpContext.Session.GetString("userrole")?.ToLower();
            var userModuleAccess = HttpContext.Session.GetString("usermoduleaccess");
            var userAccess = !string.IsNullOrEmpty(userModuleAccess) ? userModuleAccess.Split(',') : [];

            if (userRole == "admin" || userAccess.Any(module => module.Trim() == "DMS"))
            {
                return null;
            }
            TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        public IActionResult? CheckDepartmentAccess(string department)
        {
            var userAccessFolders = HttpContext.Session.GetString("useraccessfolders");
            var userRole = HttpContext.Session.GetString("userrole")?.ToLower();

            var userDepartments = userAccessFolders?.Split(',');

            if (userRole == "admin" || userDepartments == null || userDepartments.Any(dep => dep.Trim() == department))
            {
                return null;
            }
            
            TempData["ErrorMessage"] = $"You have no access to {department.Replace("_", " ")}. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        [HttpGet]
        public IActionResult UploadFile()
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }
            
            var userRole = HttpContext.Session.GetString("userrole")?.ToLower();

            if (userRole == "admin" || userRole == "uploader")
            {
                return View(new FileDocument());
            }
            
            TempData["ErrorMessage"] = "You have no access to upload a file. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(FileDocument fileDocument, IFormFile? file, CancellationToken cancellationToken)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                if (!ModelState.IsValid || file == null || file.Length == 0)
                {
                    TempData["error"] = "Please fill out all the required data.";
                    return View(fileDocument);
                }

                if (file.ContentType != "application/pdf")
                {
                    TempData["error"] = "Please upload pdf file only!";
                    return View(fileDocument);
                }

                // Increased limit for Cloud Storage (was 20MB, now 100MB)
                if (file.Length > 20000000)
                {
                    TempData["error"] = "File is too large 20MB is the maximum size allowed.";
                    return View(fileDocument);
                }

                if (await _userRepo.CheckIfFileExists(file.FileName, cancellationToken))
                {
                    TempData["error"] = "This file already exists in our database!";
                    return View(fileDocument);
                }

                var username = HttpContext.Session.GetString("username");
                var filename = Path.GetFileName(file.FileName);
                var uniquePart = $"{fileDocument.Department}_{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmssfff}";
                filename = filename.Replace("#", "");
                filename = $"{uniquePart}_{filename}";

                // Create cloud storage path
                var cloudStoragePath = fileDocument.SubCategory == "N/A"
                    ? $"Files/{fileDocument.Company}/{fileDocument.Year}/{fileDocument.Department}/{fileDocument.Category}/{filename}"
                    : $"Files/{fileDocument.Company}/{fileDocument.Year}/{fileDocument.Department}/{fileDocument.Category}/{fileDocument.SubCategory}/{filename}";

                // Upload to Cloud Storage
                var objectName = await _cloudStorage.UploadFileAsync(file, cloudStoragePath);

                fileDocument.DateUploaded = DateTimeHelper.GetCurrentPhilippineTime();
                fileDocument.Name = filename;
                fileDocument.Location = objectName; // Store cloud storage path instead of local path
                fileDocument.FileSize = file.Length;
                fileDocument.Username = username;
                fileDocument.OriginalFilename = file.FileName;
                fileDocument.IsInCloudStorage = true; // Add this property to your model if not exists
                
                await _dbContext.FileDocuments.AddAsync(fileDocument, cancellationToken);

                stopwatch.Stop();
                var duration = stopwatch.Elapsed; 
                var fileSizeInMb = (file.Length / (1024.0 * 1024.0));

                var departmentSubdirectory = fileDocument.SubCategory == "N/A"
                    ? $"{fileDocument.Company}/{fileDocument.Year}/{fileDocument.Department}/{fileDocument.Category}"
                    : $"{fileDocument.Company}/{fileDocument.Year}/{fileDocument.Department}/{fileDocument.Category}/{fileDocument.SubCategory}";

                var logs = new LogsModel(
                    username!,
                    $"Upload {file.FileName} to Cloud Storage in {departmentSubdirectory} {fileDocument.NumberOfPages} page(s). " +
                    $"Size: {fileSizeInMb:F2} MB. Duration: {duration.TotalSeconds:F2} seconds."
                );
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "File uploaded successfully to Cloud Storage";

                return View(fileDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during file upload to Cloud Storage.");
                TempData["error"] = "Contact MIS: An error occurred during file upload.";
            }

            return View(fileDocument);
        }

        public IActionResult DownloadFile()
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            // Get unique companies from database instead of file system
            var companies = _dbContext.FileDocuments
                .Where(f => !string.IsNullOrEmpty(f.Company))
                .Select(f => f.Company)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            
            return View(companies);
        }

        public IActionResult CompanyFolder(string folderName)
        {
            ViewBag.CompanyFolder = folderName;

            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            // Get unique years for the company from database
            var years = _dbContext.FileDocuments
                .Where(f => f.Company == folderName && !string.IsNullOrEmpty(f.Year))
                .Select(f => f.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            return View(years);
        }

        public IActionResult YearFolder(string companyFolderName, string yearFolderName)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;

            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            // Get unique departments for the company/year from database
            var departments = _dbContext.FileDocuments
                .Where(f => f.Company == companyFolderName && 
                           f.Year == yearFolderName && 
                           !string.IsNullOrEmpty(f.Department))
                .Select(f => f.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return View(departments);
        }

        public IActionResult DepartmentFolder(string departmentFolderName, string companyFolderName, string yearFolderName)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;

            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
            if (departmentAccessResult != null)
            {
                return departmentAccessResult;
            }

            // Get unique categories for the company/year/department from database
            var categories = _dbContext.FileDocuments
                .Where(f => f.Company == companyFolderName && 
                           f.Year == yearFolderName && 
                           f.Department == departmentFolderName &&
                           !string.IsNullOrEmpty(f.Category))
                .Select(f => f.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return View(categories);
        }

        public IActionResult SubCategoryFolder(string documentTypeFolderName, string departmentFolderName, string companyFolderName, string yearFolderName)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;
            ViewBag.DocumentTypeFolder = documentTypeFolderName;

            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            // Get unique subcategories for the specified path from database
            var subCategories = _dbContext.FileDocuments
                .Where(f => f.Company == companyFolderName && 
                           f.Year == yearFolderName && 
                           f.Department == departmentFolderName &&
                           f.Category == documentTypeFolderName &&
                           !string.IsNullOrEmpty(f.SubCategory) &&
                           f.SubCategory != "N/A")
                .Select(f => f.SubCategory)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            return View(subCategories);
        }

        public async Task<IActionResult> DisplayFiles(string departmentFolderName,
            string companyFolderName,
            string yearFolderName,
            string documentTypeFolderName,
            string? subCategoryFolder, 
            string? fileName,
            CancellationToken cancellation)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;
            ViewBag.DocumentTypeFolder = documentTypeFolderName;
            ViewBag.SubCategoryFolder = subCategoryFolder;
            ViewBag.CurrentFolder = subCategoryFolder ?? documentTypeFolderName;

            // Query from database instead of file system
            var query = _dbContext.FileDocuments
                .Where(file => file.Company == companyFolderName 
                               && file.Year == yearFolderName 
                               && file.Department == departmentFolderName
                               && file.Category == documentTypeFolderName);

            // Add subcategory filter
            if (!string.IsNullOrEmpty(subCategoryFolder))
            {
                query = query.Where(file => file.SubCategory == subCategoryFolder);
            }
            else
            {
                query = query.Where(file => file.SubCategory == "N/A" || string.IsNullOrEmpty(file.SubCategory));
            }

            // Add filename filter if specified
            if (!string.IsNullOrEmpty(fileName))
            {
                query = query.Where(file => file.Name == fileName);
            }

            var fileDocuments = await query
                .Select(file => new FileDocument
                {
                    Id = file.Id,
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
                    OriginalFilename = file.OriginalFilename,
                    FileSize = file.FileSize,
                    NumberOfPages = file.NumberOfPages
                })
                .OrderByDescending(u => u.DateUploaded)
                .ToListAsync(cancellation);

            return View(fileDocuments);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var accessCheckResult = CheckAccess();
            return accessCheckResult ?? View();
        }

        [HttpPost]
        public async Task<IActionResult> GetUploadedFiles([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var userRole = HttpContext.Session.GetString("userrole")?.ToLower();

                IEnumerable<FileDocument> files;

                if (userRole == "admin")
                {
                    files = await _userRepo.DisplayAllUploadedFiles(cancellationToken);
                }
                else
                {
                    files = await _userRepo.DisplayUploadedFiles(username!, cancellationToken);
                }

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    files = files.Where(f =>
                        f.Name!.ToLower().Contains(searchValue) ||
                        f.Description!.ToLower().Contains(searchValue) ||
                        f.DateUploaded.ToString(CultureInfo.InvariantCulture).Contains(searchValue)
                    ).ToList();
                }

                // Map to ViewModel
                var viewModel = files.Select(f => new UploadedFilesViewModel
                {
                    Id = f.Id,
                    Name = f.Name!,
                    Description = f.Description!,
                    LocationFolder = f.SubCategory == "N/A" ?
                        $"companyFolderName={f.Company}&yearFolderName={f.Year}&departmentFolderName={f.Department}&documentTypeFolderName={f.Category}&subCategoryFolder={null}&fileName={f.Name}" :
                        $"companyFolderName={f.Company}&yearFolderName={f.Year}&departmentFolderName={f.Department}&documentTypeFolderName={f.Category}&subCategoryFolder={f.SubCategory}&fileName={f.Name}",
                    UploadedBy = f.Username!,
                    DateUploaded = f.DateUploaded
                }).ToList();

                // Sorting
                if (parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    viewModel = viewModel.AsQueryable().OrderBy($"{columnName} {sortDirection}").ToList();
                }

                var totalRecords = viewModel.Count;

                // Apply pagination
                var pagedData = viewModel
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            var files = await _userRepo.GetUploadedFiles(id, cancellationToken);
            return View(files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FileDocument model, IFormFile? newFile, CancellationToken cancellationToken)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            var username = HttpContext.Session.GetString("username");
            var file = await _dbContext.FileDocuments
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (file == null)
            {
                return NotFound();
            }

            var detailsChanged = false;
            var fileChanged = false;
            var oldFileName = file.OriginalFilename;
            
            // Update file details if changed
            if (file.Description != model.Description || file.NumberOfPages != model.NumberOfPages)
            {
                file.Description = model.Description;
                file.NumberOfPages = model.NumberOfPages;
                detailsChanged = true;
            }

            if (newFile != null && newFile.Length > 0)
            {
                if (newFile.ContentType != "application/pdf")
                {
                    TempData["error"] = "Please upload pdf file only!";
                    return RedirectToAction("Edit", new { id = model.Id });
                }

                if (newFile.Length > 20000000)
                {
                    TempData["error"] = "File is too large 20MB is the maximum size allowed.";
                    return RedirectToAction("Edit", new { id = model.Id });
                }
                
                // Delete the old file from Cloud Storage
                try 
                {
                    await _cloudStorage.DeleteFileAsync(file.Location!);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not delete old file from cloud storage: {file.Location}");
                    // Continue with upload even if delete fails
                }
                
                var filename = Path.GetFileName(newFile.FileName);
                var uniquePart = $"{file.Department}_{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmssfff}";
                filename = filename.Replace("#", "");
                filename = $"{uniquePart}_{filename}";
                
                // Create new cloud storage path
                var cloudStoragePath = file.SubCategory == "N/A"
                    ? $"Files/{file.Company}/{file.Year}/{file.Department}/{file.Category}/{filename}"
                    : $"Files/{file.Company}/{file.Year}/{file.Department}/{file.Category}/{file.SubCategory}/{filename}";
                
                // Upload new file to Cloud Storage
                var objectName = await _cloudStorage.UploadFileAsync(newFile, cloudStoragePath);
                
                // Update file information
                file.Name = filename;
                file.Location = objectName;
                file.FileSize = newFile.Length;
                file.OriginalFilename = newFile.FileName;
                fileChanged = true;
            }

            if (!detailsChanged && !fileChanged)
            {
                return RedirectToAction("Edit", new { id = model.Id });
            }
            
            var changeDescription = "";

            switch (detailsChanged)
            {
                case true when fileChanged:
                    changeDescription = $"Updated details and replaced file in Cloud Storage for document# {file.Id} from {oldFileName} to {file.OriginalFilename}";
                    break;
                case true:
                    changeDescription = $"Updated details for document# {file.Id}";
                    break;
                default:
                {
                    if (fileChanged)
                    {
                        changeDescription = $"Replaced file in Cloud Storage for document# {file.Id} from {oldFileName} to {file.OriginalFilename}";
                    }
                    break;
                }
            }
            
            LogsModel logs = new(username!, changeDescription);
            await _dbContext.Logs.AddAsync(logs, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["success"] = "File document updated successfully";
            return RedirectToAction("Index");
        }

        public IActionResult GeneralSearch(string search)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            if (string.IsNullOrEmpty(search))
            {
                return RedirectToAction("Index", "Home");
            }

            var keywords = search.Split(' ');

            var result = _userRepo.SearchFile(keywords);

            return View(result);
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            if (id == 0)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username");

            var model = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x =>x.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            try
            {
                // Delete from Cloud Storage
                await _cloudStorage.DeleteFileAsync(model.Location!);

                _dbContext.Remove(model);

                LogsModel logs = new(username!, $"Delete the file from Cloud Storage: {model.Name}.");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "File has been deleted from Cloud Storage.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during file deletion from Cloud Storage.");
                TempData["error"] = "Failed to delete file from Cloud Storage.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Transfer(int id, CancellationToken cancellationToken)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            var files = await _userRepo.GetUploadedFiles(id, cancellationToken);

            return files == null ? NotFound() : View(files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(FileDocument model, CancellationToken cancellationToken)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            var username = HttpContext.Session.GetString("username");

            var existingModel = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            try
            {
                // For Cloud Storage, we need to copy the file to new location and delete old one
                var oldLocation = existingModel.Location;
                
                // Download the file from current location
                var fileStream = await _cloudStorage.DownloadFileStreamAsync(oldLocation!);
                
                // Create new filename and path
                var filename = existingModel.OriginalFilename;
                var uniquePart = $"{model.Department}_{existingModel.DateUploaded:yyyyMMddHHmmssfff}";
                filename = $"{uniquePart}_{filename}";

                var newCloudStoragePath = model.SubCategory == "N/A" 
                    ? $"Files/{model.Company}/{model.Year}/{model.Department}/{model.Category}/{filename}"
                    : $"Files/{model.Company}/{model.Year}/{model.Department}/{model.Category}/{model.SubCategory}/{filename}";

                // Convert stream to IFormFile for upload
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                var fileBytes = memoryStream.ToArray();
                
                using var newStream = new MemoryStream(fileBytes);
                var formFile = new FormFile(newStream, 0, fileBytes.Length, "file", filename)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/pdf"
                };

                // Upload to new location
                var newObjectName = await _cloudStorage.UploadFileAsync(formFile, newCloudStoragePath);

                // Delete from old location
                await _cloudStorage.DeleteFileAsync(oldLocation!);

                // Update model
                existingModel.Company = model.Company;
                existingModel.Year = model.Year;
                existingModel.Department = model.Department;
                existingModel.Category = model.Category;
                existingModel.SubCategory = model.SubCategory;
                existingModel.Name = filename;
                existingModel.Location = newObjectName;

                LogsModel logs = new(username!, $"Transfer the file in Cloud Storage: {existingModel.OriginalFilename} from {oldLocation} to {newObjectName}.");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "File successfully transferred in Cloud Storage.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during file transfer in Cloud Storage.");
                TempData["error"] = "Failed to transfer file in Cloud Storage.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Download(string filepath, string originalFilename, CancellationToken cancellationToken)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                
                // Extract department from the filepath for access check
                var pathParts = filepath.Split('/');
                if (pathParts.Length >= 4)
                {
                    var departmentFolderName = pathParts[3];
                    
                    var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
                    if (departmentAccessResult != null)
                    {
                        return departmentAccessResult;
                    }
                }

                // Create log entry
                var logs = new LogsModel(username!, $"Downloaded file from Cloud Storage: {originalFilename} from path: {filepath}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Get signed URL for direct download (better performance)
                var signedUrl = await _cloudStorage.GetSignedUrlAsync(filepath, TimeSpan.FromMinutes(5));
                return Redirect(signedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from Cloud Storage: {FilePath}", filepath);
                return BadRequest("Error downloading file from Cloud Storage");
            }
        }

        // Alternative download method that streams through the server
        [HttpGet]
        public async Task<IActionResult> DownloadDirect(string filepath, string originalFilename, CancellationToken cancellationToken)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                
                // Extract department from the filepath for access check
                var pathParts = filepath.Split('/');
                if (pathParts.Length >= 4)
                {
                    var departmentFolderName = pathParts[3];
                    
                    var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
                    if (departmentAccessResult != null)
                    {
                        return departmentAccessResult;
                    }
                }

                // Create log entry
                var logs = new LogsModel(username!, $"Downloaded file directly from Cloud Storage: {originalFilename}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Download file from Cloud Storage and stream to user
                var fileStream = await _cloudStorage.DownloadFileStreamAsync(filepath);
                return File(fileStream, "application/pdf", originalFilename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file directly from Cloud Storage: {FilePath}", filepath);
                return BadRequest("Error downloading file from Cloud Storage");
            }
        }
    }
}