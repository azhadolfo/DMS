using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Extensions;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DepartmentController> _logger;
        private readonly string? _userRole;
        private readonly string? _userName;

        public DepartmentController(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DepartmentController> logger)
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
                var departments = await _dbContext.Departments
                    .OrderBy(u => u.DepartmentName)
                    .ToListAsync();

                return View(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load departments.");
                TempData["ErrorMessage"] = "Failed to load departments.";
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
        public async Task<IActionResult> Create(DepartmentViewModel viewModel, CancellationToken cancellationToken)
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

                var departmentAlreadyExist = await _dbContext.Departments
                    .AnyAsync(u => u.DepartmentName == viewModel.DepartmentName, cancellationToken);

                if (departmentAlreadyExist)
                {
                    ModelState.AddModelError("DepartmentName", "The department with the same name already exists.");
                    TempData["ErrorMessage"] = "The department with the same name already exists.";
                    return View(viewModel);
                }

                var department = new Department
                {
                    DepartmentName = viewModel.DepartmentName.RemoveCommas(),
                    CreatedBy = _userName!,
                };

                await _dbContext.Departments.AddAsync(department, cancellationToken);

                LogsModel logs = new(_userName!, $"Add new department: {viewModel.DepartmentName}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Department created successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create department {DepartmentName}.", viewModel.DepartmentName);
                TempData["ErrorMessage"] = "Failed to create department.";
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
                var department = await _dbContext.Departments
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

                if (department == null)
                {
                    return NotFound();
                }

                var viewModel = new DepartmentViewModel
                {
                    Id = department.Id,
                    DepartmentName = department.DepartmentName.RemoveCommas(),
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load department {DepartmentId} for edit.", id);
                TempData["ErrorMessage"] = "Failed to load department.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DepartmentViewModel viewModel, CancellationToken cancellationToken)
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

                var existingDepartment = await _dbContext.Departments
                    .FirstOrDefaultAsync(x => x.Id == viewModel.Id, cancellationToken);

                if (existingDepartment == null)
                {
                    return NotFound();
                }

                var departmentAlreadyExist = await _dbContext.Departments
                    .AnyAsync(u =>
                        u.Id != viewModel.Id &&
                        u.DepartmentName == viewModel.DepartmentName, cancellationToken);

                if (departmentAlreadyExist)
                {
                    ModelState.AddModelError("DepartmentName", "The department with the same name already exists.");
                    TempData["ErrorMessage"] = "The department with the same name already exists.";
                    return View(viewModel);
                }

                var existingName = existingDepartment.DepartmentName;
                existingDepartment.DepartmentName = viewModel.DepartmentName.RemoveCommas();
                existingDepartment.EditedBy = _userName;
                existingDepartment.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                LogsModel logs = new(_userName!, $"Update department from {existingName} to {viewModel.DepartmentName}");
                await _dbContext.Logs.AddAsync(logs, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Department updated successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update department {DepartmentId}.", viewModel.Id);
                TempData["ErrorMessage"] = "Failed to update department.";
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
