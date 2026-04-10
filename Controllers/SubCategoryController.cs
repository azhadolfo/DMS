using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SubCategoryController> _logger;
        private readonly string? _userRole;
        private readonly string? _userName;

        public SubCategoryController(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SubCategoryController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;

            if (httpContextAccessor.HttpContext != null)
            {
                _userRole = httpContextAccessor.HttpContext.Session.GetString("userRole")?.ToLower();
                _userName = httpContextAccessor.HttpContext.Session.GetString("username");
            }
            else
            {
                _userRole = null;
            }
        }

        public async Task<IActionResult> Index()
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            try
            {
                var subCategory = await _dbContext.SubCategories
                    .Include(s => s.Category)
                    .OrderBy(u => u.SubCategoryName)
                    .ToListAsync();

                return View(subCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sub-categories.");
                TempData["ErrorMessage"] = "Failed to load sub-categories.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            try
            {
                var viewModel = new SubCategoryViewModel
                {
                    Categories = await _dbContext.Categories
                        .OrderBy(u => u.CategoryName)
                        .Select(c => new SelectListItem
                        {
                            Text = c.CategoryName,
                            Value = c.Id.ToString()
                        })
                        .ToListAsync(),
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sub-category create form.");
                TempData["ErrorMessage"] = "Failed to load sub-category form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                viewModel.Categories = await _dbContext.Categories
                    .OrderBy(u => u.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Text = c.CategoryName,
                        Value = c.Id.ToString()
                    })
                    .ToListAsync(cancellationToken);

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "The information you submitted is not valid.";
                    return View(viewModel);
                }

                var subCategoryAlreadyExist = await _dbContext.SubCategories
                    .AnyAsync(u => u.CategoryId == viewModel.CategoryId
                                   && u.SubCategoryName == viewModel.SubCategoryName, cancellationToken);

                if (subCategoryAlreadyExist)
                {
                    ModelState.AddModelError("SubCategoryName", "The sub-category with the same name already exists.");
                    TempData["ErrorMessage"] = "The sub-category with the same name already exists.";
                    return View(viewModel);
                }

                var subCategory = new SubCategory
                {
                    SubCategoryName = viewModel.SubCategoryName,
                    CategoryId = viewModel.CategoryId,
                    CreatedBy = _userName!,
                };

                await _dbContext.SubCategories.AddAsync(subCategory, cancellationToken);

                LogsModel logs = new(_userName!, $"Add new sub-category: {viewModel.SubCategoryName}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Sub-Category created successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create sub-category {SubCategoryName}.", viewModel.SubCategoryName);
                TempData["ErrorMessage"] = "Failed to create sub-category.";
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            try
            {
                var subCategory = await _dbContext.SubCategories
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

                if (subCategory == null)
                {
                    return NotFound();
                }

                var viewModel = new SubCategoryViewModel
                {
                    Id = subCategory.Id,
                    SubCategoryName = subCategory.SubCategoryName,
                    CategoryId = subCategory.CategoryId,
                    Categories = await _dbContext.Categories
                        .OrderBy(u => u.CategoryName)
                        .Select(c => new SelectListItem
                        {
                            Text = c.CategoryName,
                            Value = c.Id.ToString()
                        })
                        .ToListAsync(cancellationToken),
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sub-category {SubCategoryId} for edit.", id);
                TempData["ErrorMessage"] = "Failed to load sub-category.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                viewModel.Categories = await _dbContext.Categories
                    .OrderBy(u => u.CategoryName)
                    .Select(c => new SelectListItem
                    {
                        Text = c.CategoryName,
                        Value = c.Id.ToString()
                    })
                    .ToListAsync(cancellationToken);

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "The information you submitted is not valid.";
                    return View(viewModel);
                }

                var existingSubCategory = await _dbContext.SubCategories
                    .FirstOrDefaultAsync(x => x.Id == viewModel.Id, cancellationToken);

                if (existingSubCategory == null)
                {
                    return NotFound();
                }

                var subCategoryAlreadyExist = await _dbContext.SubCategories
                    .AnyAsync(u =>
                        u.Id != viewModel.Id &&
                        u.CategoryId == viewModel.CategoryId &&
                        u.SubCategoryName == viewModel.SubCategoryName, cancellationToken);

                if (subCategoryAlreadyExist)
                {
                    ModelState.AddModelError("SubCategoryName", "The sub-category with the same name already exists.");
                    TempData["ErrorMessage"] = "The sub-category with the same name already exists.";
                    return View(viewModel);
                }

                var existingName = existingSubCategory.SubCategoryName;
                existingSubCategory.SubCategoryName = viewModel.SubCategoryName;
                existingSubCategory.EditedBy = _userName;
                existingSubCategory.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                LogsModel logs = new(_userName!, $"Update sub-category from {existingName} to {viewModel.SubCategoryName}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Sub-Category updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update sub-category {SubCategoryId}.", viewModel.Id);
                TempData["ErrorMessage"] = "Failed to update sub-category.";
                return View(viewModel);
            }
        }

        private IActionResult? EnsureAdminAccess()
        {
            if (string.IsNullOrEmpty(_userName))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_userRole != "admin")
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }

            return null;
        }
    }
}
