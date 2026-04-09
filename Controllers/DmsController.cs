﻿using Document_Management.Data;
using Document_Management.Dtos;
using Document_Management.Models;
using Document_Management.Repository;
using Document_Management.Service;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Dynamic.Core;

namespace Document_Management.Controllers
{
    public class DmsController : Controller
    {
        private readonly UserRepo _userRepo;
        private readonly ILogger<DmsController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICloudStorageService _cloudStorage;
        private readonly IDmsAccessService _accessService;
        private readonly IDocumentStorageWorkflowService _documentStorageWorkflowService;

        public DmsController(
            ApplicationDbContext context,
            UserRepo userRepo,
            ILogger<DmsController> logger,
            ICloudStorageService cloudStorage,
            IDmsAccessService accessService,
            IDocumentStorageWorkflowService documentStorageWorkflowService)
        {
            _dbContext = context;
            _userRepo = userRepo;
            _logger = logger;
            _cloudStorage = cloudStorage;
            _accessService = accessService;
            _documentStorageWorkflowService = documentStorageWorkflowService;
        }

        private IActionResult? CheckDepartmentAccess(string department)
        {
            if (string.IsNullOrWhiteSpace(_accessService.Username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanAccessDepartment(department))
            {
                return null;
            }

            TempData["ErrorMessage"] = $"You have no access to {department.Replace("_", " ")}. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        private IActionResult? CheckCompanyAccess(string company)
        {
            if (string.IsNullOrWhiteSpace(_accessService.Username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanAccessCompany(company))
            {
                return null;
            }

            TempData["ErrorMessage"] = $"You have no access to {company.Replace("_", " ")}. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        private IActionResult? EnsureUploadAccess()
        {
            if (string.IsNullOrWhiteSpace(_accessService.Username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanUpload())
            {
                return null;
            }

            TempData["ErrorMessage"] = "You have no access to upload a file. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        private IActionResult? EnsureDocumentMutationAccess(FileDocument fileDocument)
        {
            if (string.IsNullOrWhiteSpace(_accessService.Username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanMutate(fileDocument))
            {
                return null;
            }

            TempData["ErrorMessage"] = "You have no access to modify this file. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        private async Task<FileDocument> GetModelSelectList(FileDocument model, CancellationToken cancellationToken)
        {
            model.Companies = await _dbContext
                .Companies
                .OrderBy(x => x.CompanyName)
                .Select(x => new SelectListItem { Text = x.CompanyName, Value = x.CompanyName })
                .ToListAsync(cancellationToken);

            var currentYear = DateTimeHelper.GetCurrentPhilippineTime().Year;
            var yearsList = new List<SelectListItem>();

            for (var i = currentYear - 20; i <= currentYear + 10; i++)
            {
                yearsList.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i.ToString() == model.Year
                });
            }

            model.Years = yearsList;

            model.Departments = await _dbContext
                .Departments
                .OrderBy(x => x.DepartmentName)
                .Select(x => new SelectListItem { Text = x.DepartmentName, Value = x.DepartmentName })
                .ToListAsync(cancellationToken);

            model.Categories = await _dbContext
                .Categories
                .OrderBy(x => x.CategoryName)
                .Select(x => new SelectListItem { Text = x.CategoryName, Value = x.CategoryName })
                .ToListAsync(cancellationToken);

            return model;
        }

        [HttpGet]
        public async Task<IActionResult> UploadFile(CancellationToken cancellationToken)
        {
            var uploadAccessResult = EnsureUploadAccess();
            if (uploadAccessResult == null)
            {
                var model = await GetModelSelectList(new FileDocument(), cancellationToken);

                return View(model);
            }

            return uploadAccessResult;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(FileDocument fileDocument, IFormFile? file, CancellationToken cancellationToken)
        {
            var uploadAccessResult = EnsureUploadAccess();
            if (uploadAccessResult != null)
            {
                return uploadAccessResult;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            fileDocument = await GetModelSelectList(fileDocument, cancellationToken);

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
                var uploadResult = await _documentStorageWorkflowService.UploadAsync(fileDocument, file, cancellationToken);

                fileDocument.DateUploaded = uploadResult.UploadedAt;
                fileDocument.Name = uploadResult.StoredFileName;
                fileDocument.Location = uploadResult.ObjectName;
                fileDocument.FileSize = uploadResult.FileSize;
                fileDocument.Username = username!;
                fileDocument.OriginalFilename = file.FileName;
                fileDocument.IsInCloudStorage = true;

                await _dbContext.FileDocuments.AddAsync(fileDocument, cancellationToken);

                stopwatch.Stop();
                var duration = stopwatch.Elapsed;
                var fileSizeInMb = (file.Length / (1024.0 * 1024.0));

                var logs = new LogsModel(
                    username!,
                    $"Upload {file.FileName} to Cloud Storage in {uploadResult.FolderPath} {fileDocument.NumberOfPages} page(s). " +
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
            if (!_accessService.IsAuthenticated())
            {
                return RedirectToAction("Login", "Account");
            }

            // Get unique companies from database instead of file system
            var companies = _dbContext.FileDocuments
                .Where(f => !string.IsNullOrEmpty(f.Company))
                .Select(f => f.Company)
                .Distinct()
                .OrderBy(c => c)
                .ToList()
                .Where(company => _accessService.CanAccessCompany(company!))
                .ToList();

            return View(companies);
        }

        public IActionResult CompanyFolder(string folderName)
        {
            ViewBag.CompanyFolder = folderName;

            var companyAccessResult = CheckCompanyAccess(folderName);
            if (companyAccessResult != null)
            {
                return companyAccessResult;
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

            var companyAccessResult = CheckCompanyAccess(companyFolderName);
            if (companyAccessResult != null)
            {
                return companyAccessResult;
            }

            // Get unique departments for the company/year from database
            var departments = _dbContext.FileDocuments
                .Where(f => f.Company == companyFolderName &&
                           f.Year == yearFolderName &&
                           !string.IsNullOrEmpty(f.Department))
                .Select(f => f.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToList()
                .Where(department => _accessService.CanAccessDepartment(department!))
                .ToList();

            return View(departments);
        }

        public IActionResult DepartmentFolder(string departmentFolderName, string companyFolderName, string yearFolderName)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;

            var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
            if (departmentAccessResult != null)
            {
                return departmentAccessResult;
            }

            // Get unique categories for the company/year/department from database
            var categories = _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == companyFolderName &&
                           f.Year == yearFolderName &&
                           f.Department == departmentFolderName &&
                           !string.IsNullOrEmpty(f.Category))
                .Select(f => new CategoryDto
                {
                    Category = f.Category,
                    SubCategory = f.SubCategory,
                })
                .Distinct()
                .OrderBy(c => c.Category)
                .ToList();

            return View(categories);
        }

        public IActionResult SubCategoryFolder(string documentTypeFolderName, string departmentFolderName, string companyFolderName, string yearFolderName)
        {
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;
            ViewBag.DocumentTypeFolder = documentTypeFolderName;

            var companyAccessResult = CheckCompanyAccess(companyFolderName);
            if (companyAccessResult != null)
            {
                return companyAccessResult;
            }

            var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
            if (departmentAccessResult != null)
            {
                return departmentAccessResult;
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
            ViewBag.CompanyFolder = companyFolderName;
            ViewBag.YearFolder = yearFolderName;
            ViewBag.DepartmentFolder = departmentFolderName;
            ViewBag.DocumentTypeFolder = documentTypeFolderName;
            ViewBag.SubCategoryFolder = subCategoryFolder;
            ViewBag.CurrentFolder = subCategoryFolder ?? documentTypeFolderName;

            var companyAccessResult = CheckCompanyAccess(companyFolderName);
            if (companyAccessResult != null)
            {
                return companyAccessResult;
            }

            var departmentAccessResult = CheckDepartmentAccess(departmentFolderName);
            if (departmentAccessResult != null)
            {
                return departmentAccessResult;
            }

            // Query from database instead of file system
            var query = _dbContext.FileDocuments
                .Where(file => file.Company == companyFolderName
                               && file.Year == yearFolderName
                               && file.Department == departmentFolderName
                               && file.Category == documentTypeFolderName
                               && !file.IsDeleted);

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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetUploadedFiles([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var userRole = HttpContext.Session.GetString("userRole")?.ToLower();

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
                    var sortDirection = orderColumn.Dir.Equals("asc", StringComparison.CurrentCultureIgnoreCase) ? "ascending" : "descending";

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
            var files = await _userRepo.GetUploadedFiles(id, cancellationToken);
            if (files == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(files);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            return View(files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FileDocument model, IFormFile? newFile, CancellationToken cancellationToken)
        {
            var username = HttpContext.Session.GetString("username");
            var file = await _dbContext.FileDocuments
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (file == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(file);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
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

            if (newFile?.Length > 0)
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

                var replaceResult = await _documentStorageWorkflowService.ReplaceFileAsync(file, newFile, cancellationToken);

                file.Name = replaceResult.StoredFileName;
                file.Location = replaceResult.ObjectName;
                file.FileSize = replaceResult.FileSize;
                file.OriginalFilename = replaceResult.OriginalFileName;
                fileChanged = true;
            }

            if (!detailsChanged && !fileChanged)
            {
                TempData["info"] = "No changes were made.";
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

        public IActionResult GeneralSearch(string search, int page = 1, int pageSize = 10, string sortBy = "DateUploaded", string sortOrder = "desc")
        {
            if (string.IsNullOrEmpty(search))
            {
                return RedirectToAction("Index", "Home");
            }

            var keywords = search.Split(' ');

            var allResults = _userRepo.SearchFile(keywords);

            // Apply sorting
            allResults = sortBy switch
            {
                "BoxNumber" => sortOrder == "asc"
                    ? allResults.OrderBy(f => f.BoxNumber).ToList()
                    : allResults.OrderByDescending(f => f.BoxNumber).ToList(),
                "OriginalFilename" => sortOrder == "asc"
                    ? allResults.OrderBy(f => f.OriginalFilename).ToList()
                    : allResults.OrderByDescending(f => f.OriginalFilename).ToList(),
                "Description" => sortOrder == "asc"
                    ? allResults.OrderBy(f => f.Description).ToList()
                    : allResults.OrderByDescending(f => f.Description).ToList(),
                "Username" => sortOrder == "asc"
                    ? allResults.OrderBy(f => f.Username).ToList()
                    : allResults.OrderByDescending(f => f.Username).ToList(),
                "DateUploaded" => sortOrder == "asc"
                    ? allResults.OrderBy(f => f.DateUploaded).ToList()
                    : allResults.OrderByDescending(f => f.DateUploaded).ToList(),
                _ => allResults.OrderByDescending(f => f.DateUploaded).ToList()
            };

            // Calculate pagination
            var totalRecords = allResults.Count;
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Get paginated results
            var paginatedResults = allResults
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass pagination data to view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedResults);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDelete(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username");

            var model = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(model);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            try
            {
                // Delete from Cloud Storage
                await _cloudStorage.DeleteFileAsync(model.Location!);

                _dbContext.Remove(model);

                LogsModel logs = new(username!, $"Permanently delete the file from Cloud Storage: {model.Name}.");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "File has been permanently deleted from Cloud Storage.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during file permanently deletion from Cloud Storage.");
                TempData["error"] = "Failed to permanently delete file from Cloud Storage.";
            }

            return RedirectToAction(nameof(Trash));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username");

            var model = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(model);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            try
            {
                model.IsDeleted = true;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username");

            var model = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(model);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            try
            {
                model.IsDeleted = false;

                LogsModel logs = new(username!, $"Restore the file from Cloud Storage: {model.Name}.");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "File has been restored from Cloud Storage.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during file restoration from Cloud Storage.");
                TempData["error"] = "Failed to restore file from Cloud Storage.";
            }

            return RedirectToAction(nameof(Trash));
        }

        [HttpGet]
        public async Task<IActionResult> Transfer(int id, CancellationToken cancellationToken)
        {
            var files = await _userRepo.GetUploadedFiles(id, cancellationToken);
            if (files == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(files);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            return View(await GetModelSelectList(files, cancellationToken));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(FileDocument model, CancellationToken cancellationToken)
        {
            var username = HttpContext.Session.GetString("username");

            var existingModel = await _dbContext
                .FileDocuments
                .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var documentAccessResult = EnsureDocumentMutationAccess(existingModel);
            if (documentAccessResult != null)
            {
                return documentAccessResult;
            }

            model = await GetModelSelectList(model, cancellationToken);

            try
            {
                var transferResult = await _documentStorageWorkflowService.TransferAsync(existingModel, model, cancellationToken);

                // Update model
                existingModel.Company = model.Company;
                existingModel.Year = model.Year;
                existingModel.Department = model.Department;
                existingModel.Category = model.Category;
                existingModel.SubCategory = model.SubCategory;
                existingModel.Name = transferResult.StoredFileName;
                existingModel.Location = transferResult.NewLocation;

                LogsModel logs = new(username!, $"Transfer the file in Cloud Storage: {existingModel.OriginalFilename} from {transferResult.OldLocation} to {transferResult.NewLocation}.");
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
        public async Task<IActionResult> Download(int documentId, string originalFilename, CancellationToken cancellationToken)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var document = await _dbContext.FileDocuments
                    .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

                if (document == null)
                {
                    return NotFound();
                }

                var companyAccessResult = CheckCompanyAccess(document.Company);
                if (companyAccessResult != null)
                {
                    return companyAccessResult;
                }

                var departmentAccessResult = CheckDepartmentAccess(document.Department!);
                if (departmentAccessResult != null)
                {
                    return departmentAccessResult;
                }

                // Create log entry
                var logs = new LogsModel(username!, $"Downloaded file from Cloud Storage: {originalFilename} from path: {document.Location}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Get signed URL for direct download (better performance)
                var signedUrl = await _cloudStorage.GetSignedUrlAsync(document.Location!, TimeSpan.FromMinutes(5));
                return Redirect(signedUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from Cloud Storage: {FileName}", originalFilename);
                return BadRequest("Error downloading file from Cloud Storage");
            }
        }

        // Alternative download method that streams through the server
        [HttpGet]
        public async Task<IActionResult> DownloadDirect(int documentId, string originalFilename, CancellationToken cancellationToken)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var document = await _dbContext.FileDocuments
                    .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);

                if (document == null)
                {
                    return NotFound();
                }

                var companyAccessResult = CheckCompanyAccess(document.Company);
                if (companyAccessResult != null)
                {
                    return companyAccessResult;
                }

                var departmentAccessResult = CheckDepartmentAccess(document.Department!);
                if (departmentAccessResult != null)
                {
                    return departmentAccessResult;
                }

                // Create log entry
                var logs = new LogsModel(username!, $"Downloaded file directly from Cloud Storage: {originalFilename}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Download file from Cloud Storage and stream to user
                var fileStream = await _cloudStorage.DownloadFileStreamAsync(document.Location!);
                return File(fileStream, "application/pdf", originalFilename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file directly from Cloud Storage: {FileName}", originalFilename);
                return BadRequest("Error downloading file from Cloud Storage");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSubCategories(string categoryName)
        {
            try
            {
                if (string.IsNullOrEmpty(categoryName))
                {
                    return Json(new List<SelectListItem>());
                }

                var subCategories = await _dbContext.SubCategories
                    .Include(x => x.Category)
                    .Where(x => x.Category.CategoryName == categoryName)
                    .Select(sc => new SelectListItem
                    {
                        Value = sc.SubCategoryName,
                        Text = sc.SubCategoryName
                    })
                    .ToListAsync();

                return Json(subCategories);
            }
            catch
            {
                return Json(new List<SelectListItem>());
            }
        }

        [HttpGet]
        public IActionResult Trash()
        {
            if (string.IsNullOrWhiteSpace(_accessService.Username))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_accessService.CanAccessTrash())
            {
                return View();
            }

            TempData["ErrorMessage"] = "You have no access to trash. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> GetDeletedFiles([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var username = HttpContext.Session.GetString("username");
                var userRole = HttpContext.Session.GetString("userRole")?.ToLower();

                IEnumerable<FileDocument> files;

                if (userRole == "admin")
                {
                    files = await _userRepo.DisplayAllDeletedFiles(cancellationToken);
                }
                else
                {
                    files = await _userRepo.DisplayAllDeletedFiles(username!, cancellationToken);
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
                    var sortDirection = orderColumn.Dir.Equals("asc", StringComparison.CurrentCultureIgnoreCase) ? "ascending" : "descending";

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
    }
}
