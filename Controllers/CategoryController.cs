using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CategoryController> _logger;
        private readonly string? _userRole;
        private readonly string? _userName;

        public CategoryController(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CategoryController> logger)
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
                var category = await _dbContext.Categories
                    .OrderBy(u => u.CategoryName)
                    .ToListAsync();

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load categories.");
                TempData["ErrorMessage"] = "Failed to load categories.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "The information you submitted is not valid.";
                    return View(viewModel);
                }

                var categoryAlreadyExist = await _dbContext.Categories
                    .AnyAsync(u => u.CategoryName == viewModel.CategoryName, cancellationToken);

                if (categoryAlreadyExist)
                {
                    ModelState.AddModelError("CategoryName", "The category with the same name already exists.");
                    TempData["ErrorMessage"] = "The category with the same name already exists.";
                    return View(viewModel);
                }

                var category = new Category
                {
                    CategoryName = viewModel.CategoryName,
                    CreatedBy = _userName!,
                };

                await _dbContext.Categories.AddAsync(category, cancellationToken);

                LogsModel logs = new(_userName!, $"Add new category: {viewModel.CategoryName}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Category created successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create category {CategoryName}.", viewModel.CategoryName);
                TempData["ErrorMessage"] = "Failed to create category.";
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
                var category = await _dbContext.Categories
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

                if (category == null)
                {
                    return NotFound();
                }

                var viewModel = new CategoryViewModel
                {
                    Id = category.Id,
                    CategoryName = category.CategoryName,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load category {CategoryId} for edit.", id);
                TempData["ErrorMessage"] = "Failed to load category.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "The information you submitted is not valid.";
                    return View(viewModel);
                }

                var existingCategory = await _dbContext.Categories
                    .FirstOrDefaultAsync(x => x.Id == viewModel.Id, cancellationToken);

                if (existingCategory == null)
                {
                    return NotFound();
                }

                var categoryAlreadyExist = await _dbContext.Categories
                    .AnyAsync(u =>
                        u.Id != viewModel.Id &&
                        u.CategoryName == viewModel.CategoryName, cancellationToken);

                if (categoryAlreadyExist)
                {
                    ModelState.AddModelError("CategoryName", "The category with the same name already exists.");
                    TempData["ErrorMessage"] = "The category with the same name already exists.";
                    return View(viewModel);
                }

                var existingName = existingCategory.CategoryName;
                existingCategory.CategoryName = viewModel.CategoryName;
                existingCategory.EditedBy = _userName;
                existingCategory.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                LogsModel logs = new(_userName!, $"Update category from {existingName} to {viewModel.CategoryName}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Category updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update category {CategoryId}.", viewModel.Id);
                TempData["ErrorMessage"] = "Failed to update category.";
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
